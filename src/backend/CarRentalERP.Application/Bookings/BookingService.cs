using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Bookings;

public sealed class BookingService
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IRepository<Booking> bookingRepository,
        IRepository<Customer> customerRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Branch> branchRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _branchRepository = branchRepository;
        _rentalRepository = rentalRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<BookingDto>> GetPagedAsync(BookingListRequest request, CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var normalizedSearch = NormalizeSearch(request.Search);
        var searchCustomerIds = await FindMatchingCustomerIdsAsync(normalizedSearch, cancellationToken);
        var searchVehicleIds = await FindMatchingVehicleIdsAsync(normalizedSearch, cancellationToken);

        var candidates = (await _bookingRepository.QueryAsync(query =>
            ApplyBookingFilters(query, request, normalizedSearch, searchCustomerIds, searchVehicleIds)
                .Select(x => new BookingListCandidate(x.Id, x.StartAtUtc, x.Status, x.QuotedTotal)), cancellationToken))
            .ToArray();

        if (candidates.Length == 0)
        {
            return new PagedResult<BookingDto>(Array.Empty<BookingDto>(), 0, page, pageSize);
        }

        var candidateBookingIds = candidates.Select(x => x.Id).ToArray();
        var statusContext = await LoadBookingStatusContextAsync(candidateBookingIds, cancellationToken);

        var filteredBookingIds = candidates
            .Where(x => request.HasOutstandingBalance is null || (CalculateOutstandingBalance(x.Id, x.QuotedTotal, statusContext.PaymentAmountsByBookingId, statusContext.LatestRentalByBookingId) > 0) == request.HasOutstandingBalance)
            .Where(x => request.HasActiveRental is null || HasActiveRental(x.Id, statusContext.LatestRentalByBookingId) == request.HasActiveRental)
            .OrderByDescending(x => x.Status == BookingStatus.Active)
            .ThenByDescending(x => HasActiveRental(x.Id, statusContext.LatestRentalByBookingId))
            .ThenByDescending(x => x.StartAtUtc)
            .Select(x => x.Id)
            .ToArray();

        var pageBookingIds = filteredBookingIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var data = await LoadContextForBookingIdsAsync(pageBookingIds, cancellationToken);
        var result = pageBookingIds
            .Select(id => data.BookingsById.GetValueOrDefault(id))
            .Where(booking => booking is not null)
            .Select(booking => Map(booking!, data))
            .ToArray();

        return new PagedResult<BookingDto>(result, filteredBookingIds.Length, page, pageSize);
    }

    public async Task<BookingDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        var data = await LoadContextForBookingIdsAsync([id], cancellationToken);
        var dto = Map(booking, data);
        return new BookingDetailDto(
            dto,
            new BookingFinancialsDto(
                booking.BaseAmount,
                booking.DiscountAmount,
                booking.DepositAmount,
                booking.QuotedTotal,
                dto.TotalPaid,
                dto.OutstandingBalance),
            new BookingOperationsDto(
                dto.HasActiveRental,
                dto.IsOverdue,
                booking.StartAtUtc,
                booking.EndAtUtc,
                BuildBookingWindowLabel(booking)));
    }

    public async Task<BookingQuoteDto> QuoteAsync(BookingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        ValidateBookingWindow(request.StartAtUtc, request.EndAtUtc, request.PricingPlan);
        ValidatePricing(request.DiscountAmount, request.DepositAmount);
        EnsureQuoteCompatible(vehicle, request.PricingPlan);

        var baseAmount = CalculateBaseAmount(vehicle, request.StartAtUtc, request.EndAtUtc, request.PricingPlan);
        if (request.DiscountAmount > baseAmount)
        {
            throw new InvalidOperationException("Discount cannot exceed base amount.");
        }

        return new BookingQuoteDto(
            request.VehicleId,
            request.StartAtUtc,
            request.EndAtUtc,
            request.PricingPlan,
            baseAmount,
            request.DiscountAmount,
            request.DepositAmount,
            baseAmount - request.DiscountAmount + request.DepositAmount);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");
        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");
        var pickupBranch = await _branchRepository.GetByIdAsync(request.PickupBranchId, cancellationToken)
            ?? throw new InvalidOperationException("Pickup branch not found.");
        var returnBranch = await _branchRepository.GetByIdAsync(request.ReturnBranchId, cancellationToken)
            ?? throw new InvalidOperationException("Return branch not found.");

        if (!pickupBranch.IsActive || !returnBranch.IsActive)
        {
            throw new InvalidOperationException("Booking branches must be active.");
        }

        ValidateBookingWindow(request.StartAtUtc, request.EndAtUtc, request.PricingPlan);
        ValidatePricing(request.DiscountAmount, request.DepositAmount);
        EnsureCustomerEligible(customer);
        EnsureVehicleEligible(vehicle);
        EnsureVehicleAtPickupBranch(vehicle, pickupBranch.Id);

        await EnsureVehicleAvailabilityAsync(request.VehicleId, request.StartAtUtc, request.EndAtUtc, null, cancellationToken);

        var baseAmount = CalculateBaseAmount(vehicle, request.StartAtUtc, request.EndAtUtc, request.PricingPlan);
        if (request.DiscountAmount > baseAmount)
        {
            throw new InvalidOperationException("Discount cannot exceed base amount.");
        }

        var bookingCount = (await _bookingRepository.QueryAsync(query => query.Select(x => x.Id), cancellationToken)).Count;

        var booking = new Booking
        {
            BookingNumber = GenerateBookingNumber(bookingCount + 1),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            PickupBranchId = request.PickupBranchId,
            ReturnBranchId = request.ReturnBranchId,
            StartAtUtc = request.StartAtUtc.ToUniversalTime(),
            EndAtUtc = request.EndAtUtc.ToUniversalTime(),
            PricingPlan = request.PricingPlan,
            BaseAmount = baseAmount,
            DiscountAmount = request.DiscountAmount,
            DepositAmount = request.DepositAmount,
            QuotedTotal = baseAmount - request.DiscountAmount + request.DepositAmount,
            Status = BookingStatus.Draft
        };

        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(booking.Id, cancellationToken)
            ?? throw new InvalidOperationException("Booking could not be reloaded.")).Booking;
    }

    public async Task<BookingDto?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Draft)
        {
            throw new InvalidOperationException("Only draft bookings can be confirmed.");
        }

        if (booking.StartAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Past-due draft booking cannot be confirmed.");
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");
        var customer = await _customerRepository.GetByIdAsync(booking.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        EnsureVehicleEligible(vehicle);
        EnsureCustomerEligible(customer);
        await EnsureVehicleAvailabilityAsync(booking.VehicleId, booking.StartAtUtc, booking.EndAtUtc, booking.Id, cancellationToken);

        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        if (vehicle.Status == VehicleStatus.Available)
        {
            vehicle.Status = VehicleStatus.Reserved;
            vehicle.UpdatedAtUtc = DateTime.UtcNow;
            await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        }

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(id, cancellationToken))?.Booking;
    }

    public async Task<BookingDto?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null)
        {
            return null;
        }

        if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Active)
        {
            throw new InvalidOperationException("This booking cannot be cancelled in its current state.");
        }

        var activeRentalExists = (await _rentalRepository.QueryAsync(query =>
            query.Where(x => x.BookingId == booking.Id && x.Status == RentalStatuses.Active)
                .Select(x => x.Id), cancellationToken)).Count > 0;
        if (activeRentalExists)
        {
            throw new InvalidOperationException("Booking with active rental cannot be cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking, cancellationToken);

        var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId, cancellationToken);
        if (vehicle is not null && vehicle.Status == VehicleStatus.Reserved)
        {
            var activeOrConfirmed = (await _bookingRepository.QueryAsync(query =>
                query.Where(x =>
                        x.Id != booking.Id &&
                        x.VehicleId == vehicle.Id &&
                        (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Active) &&
                        x.EndAtUtc > DateTime.UtcNow)
                    .Select(x => x.Id), cancellationToken)).Count > 0;

            if (!activeOrConfirmed)
            {
                vehicle.Status = VehicleStatus.Available;
                vehicle.UpdatedAtUtc = DateTime.UtcNow;
                await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(id, cancellationToken))?.Booking;
    }

    private async Task<IReadOnlyCollection<Guid>> FindMatchingCustomerIdsAsync(string? normalizedSearch, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<Guid>();
        }

        return await _customerRepository.QueryAsync(query =>
            query.Where(x =>
                    x.FullName.ToUpper().Contains(normalizedSearch) ||
                    x.Phone.ToUpper().Contains(normalizedSearch))
                .Select(x => x.Id), cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> FindMatchingVehicleIdsAsync(string? normalizedSearch, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<Guid>();
        }

        return await _vehicleRepository.QueryAsync(query =>
            query.Where(x =>
                    x.PlateNumber.ToUpper().Contains(normalizedSearch) ||
                    x.Brand.ToUpper().Contains(normalizedSearch) ||
                    x.Model.ToUpper().Contains(normalizedSearch))
                .Select(x => x.Id), cancellationToken);
    }

    private async Task<BookingStatusContext> LoadBookingStatusContextAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken)
    {
        if (bookingIds.Count == 0)
        {
            return new BookingStatusContext(
                new Dictionary<Guid, RentalTransaction>(),
                new Dictionary<Guid, decimal>());
        }

        var latestRentalByBookingId = (await _rentalRepository.QueryAsync(query =>
            query.Where(x => bookingIds.Contains(x.BookingId)), cancellationToken))
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.CheckInAtUtc ?? r.CheckOutAtUtc ?? r.CreatedAtUtc).First());

        var paymentAmountsByBookingId = (await _paymentRepository.QueryAsync(query =>
            query.Where(x =>
                    bookingIds.Contains(x.BookingId) &&
                    x.PaymentStatus == PaymentStatus.Paid), cancellationToken))
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));

        return new BookingStatusContext(latestRentalByBookingId, paymentAmountsByBookingId);
    }

    private async Task<BookingDataContext> LoadContextForBookingIdsAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken)
    {
        if (bookingIds.Count == 0)
        {
            return new BookingDataContext(
                new Dictionary<Guid, Booking>(),
                new Dictionary<Guid, Customer>(),
                new Dictionary<Guid, Vehicle>(),
                new Dictionary<Guid, Branch>(),
                new Dictionary<Guid, RentalTransaction>(),
                new Dictionary<Guid, decimal>());
        }

        var bookings = (await _bookingRepository.QueryAsync(query =>
            query.Where(x => bookingIds.Contains(x.Id)), cancellationToken))
            .ToArray();
        var customerIds = bookings.Select(x => x.CustomerId).Distinct().ToArray();
        var vehicleIds = bookings.Select(x => x.VehicleId).Distinct().ToArray();
        var branchIds = bookings.SelectMany(x => new[] { x.PickupBranchId, x.ReturnBranchId }).Distinct().ToArray();

        var customers = (await _customerRepository.QueryAsync(query =>
            query.Where(x => customerIds.Contains(x.Id)), cancellationToken))
            .ToDictionary(x => x.Id);
        var vehicles = (await _vehicleRepository.QueryAsync(query =>
            query.Where(x => vehicleIds.Contains(x.Id)), cancellationToken))
            .ToDictionary(x => x.Id);
        var branches = (await _branchRepository.QueryAsync(query =>
            query.Where(x => branchIds.Contains(x.Id)), cancellationToken))
            .ToDictionary(x => x.Id);
        var latestRentalByBookingId = (await _rentalRepository.QueryAsync(query =>
            query.Where(x => bookingIds.Contains(x.BookingId)), cancellationToken))
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.CheckInAtUtc ?? r.CheckOutAtUtc ?? r.CreatedAtUtc).First());
        var paymentAmountsByBookingId = (await _paymentRepository.QueryAsync(query =>
            query.Where(x =>
                    bookingIds.Contains(x.BookingId) &&
                    x.PaymentStatus == PaymentStatus.Paid), cancellationToken))
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));

        return new BookingDataContext(
            bookings.ToDictionary(x => x.Id),
            customers,
            vehicles,
            branches,
            latestRentalByBookingId,
            paymentAmountsByBookingId);
    }

    private async Task EnsureVehicleAvailabilityAsync(
        Guid vehicleId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        Guid? currentBookingId,
        CancellationToken cancellationToken)
    {
        var overlapExists = (await _bookingRepository.QueryAsync(query =>
            query.Where(x =>
                    x.VehicleId == vehicleId &&
                    x.Id != currentBookingId &&
                    (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Active) &&
                    !(x.EndAtUtc <= startAtUtc || x.StartAtUtc >= endAtUtc))
                .Select(x => x.Id), cancellationToken)).Count > 0;

        if (overlapExists)
        {
            throw new InvalidOperationException("Vehicle is not available for the selected period.");
        }
    }

    private static BookingDto Map(Booking booking, BookingDataContext data)
    {
        var customer = data.CustomersById.GetValueOrDefault(booking.CustomerId)
            ?? throw new InvalidOperationException("Booking customer mapping is missing.");
        var vehicle = data.VehiclesById.GetValueOrDefault(booking.VehicleId)
            ?? throw new InvalidOperationException("Booking vehicle mapping is missing.");
        var pickupBranch = data.BranchesById.GetValueOrDefault(booking.PickupBranchId)
            ?? throw new InvalidOperationException("Booking pickup branch mapping is missing.");
        var returnBranch = data.BranchesById.GetValueOrDefault(booking.ReturnBranchId)
            ?? throw new InvalidOperationException("Booking return branch mapping is missing.");

        return new BookingDto(
            booking.Id,
            booking.BookingNumber,
            booking.CustomerId,
            customer.FullName,
            booking.VehicleId,
            $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
            booking.PickupBranchId,
            pickupBranch.Name,
            booking.ReturnBranchId,
            returnBranch.Name,
            booking.StartAtUtc,
            booking.EndAtUtc,
            booking.PricingPlan,
            booking.BaseAmount,
            booking.DiscountAmount,
            booking.DepositAmount,
            booking.QuotedTotal,
            data.PaymentAmountsByBookingId.GetValueOrDefault(booking.Id),
            CalculateOutstandingBalance(booking.Id, booking.QuotedTotal, data.PaymentAmountsByBookingId, data.LatestRentalByBookingId),
            HasActiveRental(booking.Id, data.LatestRentalByBookingId),
            IsOverdue(booking.Id, booking.EndAtUtc, data.LatestRentalByBookingId),
            booking.Status,
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc);
    }

    private static IQueryable<Booking> ApplyBookingFilters(
        IQueryable<Booking> query,
        BookingListRequest request,
        string? normalizedSearch,
        IReadOnlyCollection<Guid> searchCustomerIds,
        IReadOnlyCollection<Guid> searchVehicleIds)
    {
        query = query
            .Where(x => !request.Status.HasValue || x.Status == request.Status.Value)
            .Where(x => !request.BranchId.HasValue || x.PickupBranchId == request.BranchId.Value || x.ReturnBranchId == request.BranchId.Value)
            .Where(x => !request.CustomerId.HasValue || x.CustomerId == request.CustomerId.Value)
            .Where(x => !request.VehicleId.HasValue || x.VehicleId == request.VehicleId.Value)
            .Where(x => !request.StartFromUtc.HasValue || x.StartAtUtc >= request.StartFromUtc.Value)
            .Where(x => !request.StartToUtc.HasValue || x.StartAtUtc <= request.StartToUtc.Value);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                x.BookingNumber.ToUpper().Contains(normalizedSearch) ||
                searchCustomerIds.Contains(x.CustomerId) ||
                searchVehicleIds.Contains(x.VehicleId));
        }

        return query;
    }

    private static void ValidateListRequest(BookingListRequest request)
    {
        if (request.StartFromUtc.HasValue && request.StartToUtc.HasValue && request.StartFromUtc > request.StartToUtc)
        {
            throw new InvalidOperationException("Start from date cannot be after start to date.");
        }
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToUpperInvariant();

    private static void ValidateBookingWindow(DateTime startAtUtc, DateTime endAtUtc, PricingPlan pricingPlan)
    {
        var normalizedStart = startAtUtc.ToUniversalTime();
        var normalizedEnd = endAtUtc.ToUniversalTime();

        if (normalizedEnd <= normalizedStart)
        {
            throw new InvalidOperationException("End date must be after start date.");
        }

        if (normalizedStart < DateTime.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Booking start date cannot be in the past.");
        }

        var duration = normalizedEnd - normalizedStart;
        if (pricingPlan == PricingPlan.Hourly && duration.TotalHours < 1)
        {
            throw new InvalidOperationException("Hourly bookings must be at least 1 hour.");
        }

        if (pricingPlan == PricingPlan.Daily && duration.TotalDays < 1)
        {
            throw new InvalidOperationException("Daily bookings must be at least 1 day.");
        }
    }

    private static void ValidatePricing(decimal discountAmount, decimal depositAmount)
    {
        if (discountAmount < 0)
        {
            throw new InvalidOperationException("Discount amount cannot be negative.");
        }

        if (depositAmount < 0)
        {
            throw new InvalidOperationException("Deposit amount cannot be negative.");
        }
    }

    private static void EnsureCustomerEligible(Customer customer)
    {
        if (!customer.IsActive)
        {
            throw new InvalidOperationException("Inactive customers cannot create bookings.");
        }

        if (customer.VerificationStatus == VerificationStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected customers cannot create bookings.");
        }

        if (customer.LicenseExpiry is not null && customer.LicenseExpiry <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new InvalidOperationException("Customer license is expired.");
        }
    }

    private static void EnsureVehicleEligible(Vehicle vehicle)
    {
        if (vehicle.Status == VehicleStatus.ActiveRental || vehicle.Status == VehicleStatus.Maintenance || vehicle.Status == VehicleStatus.OutOfService)
        {
            throw new InvalidOperationException("Vehicle is not eligible for booking.");
        }
    }

    private static void EnsureQuoteCompatible(Vehicle vehicle, PricingPlan pricingPlan)
    {
        if (pricingPlan == PricingPlan.Daily && vehicle.DailyRate <= 0)
        {
            throw new InvalidOperationException("Vehicle does not support daily pricing.");
        }

        if (pricingPlan == PricingPlan.Hourly && vehicle.HourlyRate <= 0)
        {
            throw new InvalidOperationException("Vehicle does not support hourly pricing.");
        }
    }

    private static void EnsureVehicleAtPickupBranch(Vehicle vehicle, Guid pickupBranchId)
    {
        if (vehicle.BranchId != pickupBranchId)
        {
            throw new InvalidOperationException("Vehicle must belong to the pickup branch.");
        }
    }

    private static decimal CalculateBaseAmount(Vehicle vehicle, DateTime startAtUtc, DateTime endAtUtc, PricingPlan pricingPlan)
    {
        var durationHours = (decimal)Math.Ceiling((endAtUtc.ToUniversalTime() - startAtUtc.ToUniversalTime()).TotalHours);
        return pricingPlan == PricingPlan.Daily
            ? Math.Ceiling(durationHours / 24m) * vehicle.DailyRate
            : durationHours * vehicle.HourlyRate;
    }

    private static decimal CalculateOutstandingBalance(
        Guid bookingId,
        decimal quotedTotal,
        IReadOnlyDictionary<Guid, decimal> paymentAmountsByBookingId,
        IReadOnlyDictionary<Guid, RentalTransaction> latestRentalByBookingId)
    {
        var totalDue = latestRentalByBookingId.GetValueOrDefault(bookingId)?.FinalAmount;
        var bookingTotal = totalDue is > 0 ? totalDue.Value : quotedTotal;
        return Math.Max(0, bookingTotal - paymentAmountsByBookingId.GetValueOrDefault(bookingId));
    }

    private static bool HasActiveRental(Guid bookingId, IReadOnlyDictionary<Guid, RentalTransaction> latestRentalByBookingId) =>
        latestRentalByBookingId.GetValueOrDefault(bookingId)?.Status == RentalStatuses.Active;

    private static bool IsOverdue(Guid bookingId, DateTime endAtUtc, IReadOnlyDictionary<Guid, RentalTransaction> latestRentalByBookingId) =>
        HasActiveRental(bookingId, latestRentalByBookingId) && endAtUtc < DateTime.UtcNow;

    private static string BuildBookingWindowLabel(Booking booking) =>
        booking.StartAtUtc > DateTime.UtcNow
            ? "Upcoming"
            : booking.EndAtUtc < DateTime.UtcNow
                ? "Expired"
                : "In progress window";

    private static string GenerateBookingNumber(int sequence) =>
        $"BK-{DateTime.UtcNow:yyyyMMdd}-{sequence:0000}";

    private sealed record BookingDataContext(
        IReadOnlyDictionary<Guid, Booking> BookingsById,
        IReadOnlyDictionary<Guid, Customer> CustomersById,
        IReadOnlyDictionary<Guid, Vehicle> VehiclesById,
        IReadOnlyDictionary<Guid, Branch> BranchesById,
        IReadOnlyDictionary<Guid, RentalTransaction> LatestRentalByBookingId,
        IReadOnlyDictionary<Guid, decimal> PaymentAmountsByBookingId);

    private sealed record BookingStatusContext(
        IReadOnlyDictionary<Guid, RentalTransaction> LatestRentalByBookingId,
        IReadOnlyDictionary<Guid, decimal> PaymentAmountsByBookingId);

    private sealed record BookingListCandidate(Guid Id, DateTime StartAtUtc, BookingStatus Status, decimal QuotedTotal);
}
