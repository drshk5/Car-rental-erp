using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Rentals;

public sealed class RentalService
{
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RentalService(
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Booking> bookingRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Customer> customerRepository,
        IRepository<Branch> branchRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _rentalRepository = rentalRepository;
        _bookingRepository = bookingRepository;
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RentalListResponse> GetPagedAsync(RentalListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var filtered = FilterRentals(data.Rentals, request, data)
            .OrderByDescending(IsActive)
            .ThenByDescending(x => IsOverdue(x, data.BookingsById[x.BookingId]))
            .ThenByDescending(x => x.CheckOutAtUtc ?? x.CreatedAtUtc)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var pageItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        var summary = new RentalSummaryDto(
            filtered.Length,
            filtered.Count(IsActive),
            filtered.Count(IsCompleted),
            filtered.Count(x => IsOverdue(x, data.BookingsById[x.BookingId])),
            filtered.Sum(x => CalculateOutstandingBalance(x, data.PaymentAmountsByBookingId, data.BookingsById[x.BookingId])));

        return new RentalListResponse(
            new PagedResult<RentalDto>(pageItems, filtered.Length, page, pageSize),
            summary);
    }

    public async Task<IReadOnlyCollection<RentalDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        return data.Rentals
            .Where(IsActive)
            .OrderByDescending(x => x.CheckOutAtUtc ?? x.CreatedAtUtc)
            .Select(x => Map(x, data))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<RentalDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        return data.Rentals
            .Where(x => IsOverdue(x, data.BookingsById[x.BookingId]))
            .OrderBy(x => data.BookingsById[x.BookingId].EndAtUtc)
            .Select(x => Map(x, data))
            .ToArray();
    }

    public async Task<RentalStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidPayments = data.Payments
            .Where(x => x.PaymentStatus == PaymentStatus.Paid)
            .ToArray();

        var completedRentals = data.Rentals
            .Where(IsCompleted)
            .ToArray();

        var averageDurationDays = completedRentals.Length == 0
            ? 0
            : Math.Round(completedRentals
                .Where(x => x.CheckOutAtUtc.HasValue && x.CheckInAtUtc.HasValue)
                .DefaultIfEmpty()
                .Average(x => x is null || !x.CheckOutAtUtc.HasValue || !x.CheckInAtUtc.HasValue
                    ? 0
                    : Math.Max(0, (x.CheckInAtUtc.Value - x.CheckOutAtUtc.Value).TotalDays)), 1);

        return new RentalStatsDto(
            data.Rentals.Count,
            data.Rentals.Count(IsActive),
            completedRentals.Length,
            data.Rentals.Count(x => IsOverdue(x, data.BookingsById[x.BookingId])),
            data.Rentals.Count(x => x.CheckOutAtUtc.HasValue && x.CheckOutAtUtc.Value.Date == now.Date),
            data.Rentals.Count(x => x.CheckInAtUtc.HasValue && x.CheckInAtUtc.Value.Date == now.Date),
            data.Rentals.Sum(x => CalculateOutstandingBalance(x, data.PaymentAmountsByBookingId, data.BookingsById[x.BookingId])),
            paidPayments.Where(x => x.PaidAtUtc.Date == now.Date).Sum(x => x.Amount),
            paidPayments.Where(x => x.PaidAtUtc >= weekStart).Sum(x => x.Amount),
            paidPayments.Where(x => x.PaidAtUtc >= monthStart).Sum(x => x.Amount),
            (decimal)averageDurationDays);
    }

    public async Task<RentalDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var upcomingReturns = data.Rentals
            .Where(IsActive)
            .Where(x => data.BookingsById[x.BookingId].EndAtUtc >= now)
            .OrderBy(x => data.BookingsById[x.BookingId].EndAtUtc)
            .Take(5)
            .Select(x => Map(x, data))
            .ToArray();

        var recentCheckouts = data.Rentals
            .Where(x => x.CheckOutAtUtc.HasValue)
            .OrderByDescending(x => x.CheckOutAtUtc)
            .Take(5)
            .Select(x => Map(x, data))
            .ToArray();

        return new RentalDashboardSummaryDto(
            data.Rentals.Count(IsActive),
            data.Rentals.Count(x => IsOverdue(x, data.BookingsById[x.BookingId])),
            data.Rentals.Count(x => x.CheckOutAtUtc.HasValue && x.CheckOutAtUtc.Value.Date == now.Date),
            data.Rentals.Count(x => x.CheckInAtUtc.HasValue && x.CheckInAtUtc.Value.Date == now.Date),
            upcomingReturns,
            recentCheckouts);
    }

    public async Task<RentalDto> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCheckoutRequest(request);

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be checked out.");
        }

        EnsureBookingReadyForCheckout(booking);

        var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var customer = await _customerRepository.GetByIdAsync(booking.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        EnsureCustomerEligible(customer);

        if (vehicle.Status != VehicleStatus.Reserved)
        {
            throw new InvalidOperationException("Vehicle must be reserved before checkout.");
        }

        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        if (rentals.Any(x => x.BookingId == request.BookingId && IsActive(x)))
        {
            throw new InvalidOperationException("An active rental already exists for this booking.");
        }

        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var vehicleIdsByBooking = bookings.ToDictionary(x => x.Id, x => x.VehicleId);
        if (rentals.Any(x =>
                x.BookingId != request.BookingId &&
                IsActive(x) &&
                vehicleIdsByBooking.TryGetValue(x.BookingId, out var bookedVehicleId) &&
                bookedVehicleId == vehicle.Id))
        {
            throw new InvalidOperationException("Vehicle already has another active rental.");
        }

        var rental = new RentalTransaction
        {
            BookingId = booking.Id,
            CheckOutAtUtc = DateTime.UtcNow,
            OdometerOut = request.OdometerOut,
            FuelOut = request.FuelOut.Trim(),
            DamageNotes = request.Notes.Trim(),
            FinalAmount = booking.QuotedTotal,
            Status = RentalStatuses.Active
        };

        booking.Status = BookingStatus.Active;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        vehicle.Status = VehicleStatus.ActiveRental;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _rentalRepository.AddAsync(rental, cancellationToken);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetMappedRentalAsync(rental.Id, cancellationToken);
    }

    public async Task<RentalDto?> CheckinAsync(Guid id, CheckinRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCheckinRequest(request);

        var rental = await _rentalRepository.GetByIdAsync(id, cancellationToken);
        if (rental is null)
        {
            return null;
        }

        if (!IsActive(rental))
        {
            throw new InvalidOperationException("Only active rentals can be checked in.");
        }

        if (request.OdometerIn < rental.OdometerOut)
        {
            throw new InvalidOperationException("Odometer in cannot be less than odometer out.");
        }

        var booking = await _bookingRepository.GetByIdAsync(rental.BookingId, cancellationToken)
            ?? throw new InvalidOperationException("Booking not found.");

        var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        rental.CheckInAtUtc = DateTime.UtcNow;
        rental.OdometerIn = request.OdometerIn;
        rental.FuelIn = request.FuelIn.Trim();
        rental.ExtraCharges = request.ExtraCharges;
        rental.DamageNotes = request.DamageNotes.Trim();
        rental.FinalAmount = booking.QuotedTotal + request.ExtraCharges;
        rental.Status = RentalStatuses.Completed;
        rental.UpdatedAtUtc = DateTime.UtcNow;

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        vehicle.Status = VehicleStatus.Available;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _rentalRepository.UpdateAsync(rental, cancellationToken);
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetMappedRentalAsync(rental.Id, cancellationToken);
    }

    public async Task<RentalDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var rental = data.Rentals.FirstOrDefault(x => x.Id == id);
        if (rental is null)
        {
            return null;
        }

        var booking = data.BookingsById[rental.BookingId];
        var rentalDto = Map(rental, data);
        return new RentalDetailDto(
            rentalDto,
            new RentalFinancialsDto(
                booking.QuotedTotal,
                rental.ExtraCharges,
                rental.FinalAmount,
                rentalDto.TotalPaid,
                rentalDto.OutstandingBalance),
            new RentalTimelineDto(
                booking.StartAtUtc,
                booking.EndAtUtc,
                rental.CheckOutAtUtc,
                rental.CheckInAtUtc,
                rentalDto.IsOverdue));
    }

    public async Task<RentalDto?> UpdateDamageNotesAsync(Guid id, UpdateRentalDamageRequest request, CancellationToken cancellationToken = default)
    {
        var rental = await _rentalRepository.GetByIdAsync(id, cancellationToken);
        if (rental is null)
        {
            return null;
        }

        rental.DamageNotes = request.DamageNotes.Trim();
        rental.UpdatedAtUtc = DateTime.UtcNow;

        await _rentalRepository.UpdateAsync(rental, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetMappedRentalAsync(rental.Id, cancellationToken);
    }

    private async Task<RentalDto> GetMappedRentalAsync(Guid rentalId, CancellationToken cancellationToken)
    {
        var data = await LoadContextAsync(cancellationToken);
        var rental = data.Rentals.FirstOrDefault(x => x.Id == rentalId)
            ?? throw new InvalidOperationException("Rental not found after save.");
        return Map(rental, data);
    }

    private async Task<RentalDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var bookings = (await _bookingRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var customers = (await _customerRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var vehicles = (await _vehicleRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var branches = (await _branchRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var payments = (await _paymentRepository.ListAsync(cancellationToken)).ToArray();
        var paymentAmountsByBookingId = payments
            .Where(x => x.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));

        return new RentalDataContext(rentals, bookings, customers, vehicles, branches, paymentAmountsByBookingId, payments);
    }

    private static IEnumerable<RentalTransaction> FilterRentals(
        IReadOnlyCollection<RentalTransaction> rentals,
        RentalListRequest request,
        RentalDataContext data)
    {
        return rentals
            .Where(x => string.IsNullOrWhiteSpace(request.Status) || x.Status.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => request.CustomerId is null || data.BookingsById[x.BookingId].CustomerId == request.CustomerId)
            .Where(x => request.VehicleId is null || data.BookingsById[x.BookingId].VehicleId == request.VehicleId)
            .Where(x => request.BranchId is null || data.BookingsById[x.BookingId].PickupBranchId == request.BranchId || data.BookingsById[x.BookingId].ReturnBranchId == request.BranchId)
            .Where(x => request.IsOverdue is null || IsOverdue(x, data.BookingsById[x.BookingId]) == request.IsOverdue)
            .Where(x => MatchesSearch(x, request.Search, data));
    }

    private static bool MatchesSearch(RentalTransaction rental, string? search, RentalDataContext data)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        var booking = data.BookingsById[rental.BookingId];
        var customer = data.CustomersById[booking.CustomerId];
        var vehicle = data.VehiclesById[booking.VehicleId];

        return booking.BookingNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               customer.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               customer.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.PlateNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.Model.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static RentalDto Map(RentalTransaction rental, RentalDataContext data)
    {
        var booking = data.BookingsById.GetValueOrDefault(rental.BookingId)
            ?? throw new InvalidOperationException("Rental booking mapping is missing.");
        var customer = data.CustomersById.GetValueOrDefault(booking.CustomerId)
            ?? throw new InvalidOperationException("Rental customer mapping is missing.");
        var vehicle = data.VehiclesById.GetValueOrDefault(booking.VehicleId)
            ?? throw new InvalidOperationException("Rental vehicle mapping is missing.");
        var pickupBranch = data.BranchesById.GetValueOrDefault(booking.PickupBranchId)
            ?? throw new InvalidOperationException("Rental pickup branch mapping is missing.");
        var returnBranch = data.BranchesById.GetValueOrDefault(booking.ReturnBranchId)
            ?? throw new InvalidOperationException("Rental return branch mapping is missing.");

        var totalPaid = data.PaymentAmountsByBookingId.GetValueOrDefault(booking.Id);
        var outstandingBalance = CalculateOutstandingBalance(rental, data.PaymentAmountsByBookingId, booking);

        return new RentalDto(
            rental.Id,
            booking.Id,
            booking.BookingNumber,
            customer.Id,
            customer.FullName,
            customer.Phone,
            customer.Email,
            vehicle.Id,
            $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
            vehicle.PlateNumber,
            vehicle.Vin,
            booking.PickupBranchId,
            pickupBranch.Name,
            booking.ReturnBranchId,
            returnBranch.Name,
            booking.StartAtUtc,
            booking.EndAtUtc,
            rental.CheckOutAtUtc,
            rental.CheckInAtUtc,
            rental.OdometerOut,
            rental.OdometerIn,
            rental.OdometerIn.HasValue ? rental.OdometerIn.Value - rental.OdometerOut : 0,
            rental.FuelOut,
            rental.FuelIn,
            rental.ExtraCharges,
            rental.DamageNotes,
            rental.FinalAmount,
            totalPaid,
            outstandingBalance,
            IsOverdue(rental, booking),
            rental.Status,
            rental.CreatedAtUtc,
            rental.UpdatedAtUtc);
    }

    private static decimal CalculateOutstandingBalance(
        RentalTransaction rental,
        IReadOnlyDictionary<Guid, decimal> paymentAmountsByBookingId,
        Booking booking)
    {
        var totalDue = rental.FinalAmount > 0 ? rental.FinalAmount : booking.QuotedTotal;
        return Math.Max(0, totalDue - paymentAmountsByBookingId.GetValueOrDefault(booking.Id));
    }

    private static bool IsActive(RentalTransaction rental) =>
        rental.Status.Equals(RentalStatuses.Active, StringComparison.OrdinalIgnoreCase);

    private static bool IsCompleted(RentalTransaction rental) =>
        rental.Status.Equals(RentalStatuses.Completed, StringComparison.OrdinalIgnoreCase);

    private static bool IsOverdue(RentalTransaction rental, Booking booking) =>
        IsActive(rental) && booking.EndAtUtc < DateTime.UtcNow;

    private static void ValidateCheckoutRequest(CheckoutRequest request)
    {
        if (request.OdometerOut < 0)
        {
            throw new InvalidOperationException("Odometer out must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(request.FuelOut))
        {
            throw new InvalidOperationException("Fuel out is required.");
        }
    }

    private static void ValidateCheckinRequest(CheckinRequest request)
    {
        if (request.OdometerIn < 0)
        {
            throw new InvalidOperationException("Odometer in must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(request.FuelIn))
        {
            throw new InvalidOperationException("Fuel in is required.");
        }

        if (request.ExtraCharges < 0)
        {
            throw new InvalidOperationException("Extra charges cannot be negative.");
        }
    }

    private static void EnsureBookingReadyForCheckout(Booking booking)
    {
        if (booking.EndAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Booking window has already expired.");
        }
    }

    private static void EnsureCustomerEligible(Customer customer)
    {
        if (!customer.IsActive)
        {
            throw new InvalidOperationException("Inactive customer cannot start a rental.");
        }

        if (customer.VerificationStatus != VerificationStatus.Verified)
        {
            throw new InvalidOperationException("Customer must be verified before checkout.");
        }

        if (customer.LicenseExpiry.HasValue && customer.LicenseExpiry.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Customer driving license has expired.");
        }
    }

    private sealed record RentalDataContext(
        IReadOnlyCollection<RentalTransaction> Rentals,
        IReadOnlyDictionary<Guid, Booking> BookingsById,
        IReadOnlyDictionary<Guid, Customer> CustomersById,
        IReadOnlyDictionary<Guid, Vehicle> VehiclesById,
        IReadOnlyDictionary<Guid, Branch> BranchesById,
        IReadOnlyDictionary<Guid, decimal> PaymentAmountsByBookingId,
        IReadOnlyCollection<Payment> Payments);
}
