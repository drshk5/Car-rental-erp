using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Owners;

public sealed class OwnerService
{
    private readonly IRepository<Owner> _ownerRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OwnerService(
        IRepository<Owner> ownerRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Booking> bookingRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _ownerRepository = ownerRepository;
        _vehicleRepository = vehicleRepository;
        _bookingRepository = bookingRepository;
        _rentalRepository = rentalRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<OwnerDto>> GetPagedAsync(OwnerListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var filtered = data.Owners
            .Where(x => request.IsActive is null || x.IsActive == request.IsActive)
            .Where(x => request.HasVehicles is null || data.VehicleCountsByOwnerId.GetValueOrDefault(x.Id) > 0 == request.HasVehicles)
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.DisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.ContactName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.Email.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => data.GrossRevenueByOwnerId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.DisplayName)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        return new PagedResult<OwnerDto>(items, filtered.Length, page, pageSize);
    }

    public async Task<OwnerDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var owner = data.Owners.FirstOrDefault(x => x.Id == id);
        if (owner is null)
        {
            return null;
        }

        var dto = Map(owner, data);
        return new OwnerDetailDto(
            dto,
            new OwnerPortfolioDto(
                dto.VehicleCount,
                dto.ActiveRentalCount,
                dto.CompletedBookingCount,
                data.ActiveVehicleCountsByOwnerId.GetValueOrDefault(owner.Id)),
            new OwnerRevenueSplitDto(
                dto.GrossRevenue,
                dto.PartnerShareAmount,
                dto.CompanyShareAmount,
                owner.RevenueSharePercentage));
    }

    public async Task<OwnerDto> CreateAsync(CreateOwnerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.DisplayName, request.ContactName, request.Email, request.Phone, request.RevenueSharePercentage);
        var owners = await _ownerRepository.ListAsync(cancellationToken);
        EnsureUnique(owners, request.DisplayName, request.Email, null);

        var owner = new Owner
        {
            DisplayName = request.DisplayName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            RevenueSharePercentage = request.RevenueSharePercentage
        };

        await _ownerRepository.AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(owner.Id, cancellationToken)
            ?? throw new InvalidOperationException("Owner could not be reloaded.")).Profile;
    }

    public async Task<OwnerDto?> UpdateAsync(Guid id, UpdateOwnerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.DisplayName, request.ContactName, request.Email, request.Phone, request.RevenueSharePercentage);
        var owner = await _ownerRepository.GetByIdAsync(id, cancellationToken);
        if (owner is null)
        {
            return null;
        }

        var owners = await _ownerRepository.ListAsync(cancellationToken);
        EnsureUnique(owners, request.DisplayName, request.Email, id);

        owner.DisplayName = request.DisplayName.Trim();
        owner.ContactName = request.ContactName.Trim();
        owner.Email = request.Email.Trim();
        owner.Phone = request.Phone.Trim();
        owner.RevenueSharePercentage = request.RevenueSharePercentage;
        owner.UpdatedAtUtc = DateTime.UtcNow;

        await _ownerRepository.UpdateAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(owner.Id, cancellationToken))?.Profile;
    }

    public async Task<OwnerDto?> SetStatusAsync(Guid id, SetOwnerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await _ownerRepository.GetByIdAsync(id, cancellationToken);
        if (owner is null)
        {
            return null;
        }

        if (!request.IsActive)
        {
            var vehicles = await _vehicleRepository.ListAsync(cancellationToken);
            var ownerVehicles = vehicles.Where(x => x.OwnerId == id).ToArray();

            if (ownerVehicles.Any(x => x.Status is VehicleStatus.ActiveRental or VehicleStatus.Reserved))
            {
                throw new InvalidOperationException("Owner with reserved or active-rental vehicles cannot be deactivated.");
            }
        }

        owner.IsActive = request.IsActive;
        owner.UpdatedAtUtc = DateTime.UtcNow;
        await _ownerRepository.UpdateAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(owner.Id, cancellationToken))?.Profile;
    }

    public async Task<IReadOnlyCollection<OwnerRevenueDto>> GetRevenueSummaryAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        return data.Owners
            .OrderByDescending(x => data.GrossRevenueByOwnerId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.DisplayName)
            .Select(x =>
            {
                var ownerDto = Map(x, data);
                return new OwnerRevenueDto(
                    ownerDto.Id,
                    ownerDto.DisplayName,
                    ownerDto.VehicleCount,
                    ownerDto.ActiveRentalCount,
                    ownerDto.CompletedBookingCount,
                    ownerDto.GrossRevenue,
                    ownerDto.PartnerShareAmount,
                    ownerDto.CompanyShareAmount);
            })
            .ToArray();
    }

    private async Task<OwnerDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var owners = await _ownerRepository.ListAsync(cancellationToken);
        var vehicles = await _vehicleRepository.ListAsync(cancellationToken);
        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var payments = await _paymentRepository.ListAsync(cancellationToken);

        var vehicleCountsByOwnerId = vehicles
            .GroupBy(x => x.OwnerId)
            .ToDictionary(x => x.Key, x => x.Count());

        var activeVehicleCountsByOwnerId = vehicles
            .Where(x => x.Status == VehicleStatus.Available)
            .GroupBy(x => x.OwnerId)
            .ToDictionary(x => x.Key, x => x.Count());

        var ownerIdsByVehicleId = vehicles.ToDictionary(x => x.Id, x => x.OwnerId);

        var bookingsByOwnerId = bookings
            .Where(x => ownerIdsByVehicleId.ContainsKey(x.VehicleId))
            .GroupBy(x => ownerIdsByVehicleId[x.VehicleId])
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Booking>)x.ToArray());

        var activeRentalCountsByOwnerId = rentals
            .Where(x => x.Status == RentalStatuses.Active)
            .Join(bookings, rental => rental.BookingId, booking => booking.Id, (rental, booking) => booking)
            .Where(booking => ownerIdsByVehicleId.ContainsKey(booking.VehicleId))
            .GroupBy(booking => ownerIdsByVehicleId[booking.VehicleId])
            .ToDictionary(x => x.Key, x => x.Count());

        var grossRevenueByOwnerId = payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .Join(bookings, payment => payment.BookingId, booking => booking.Id, (payment, booking) => new { payment.Amount, booking.VehicleId })
            .Where(x => ownerIdsByVehicleId.ContainsKey(x.VehicleId))
            .GroupBy(x => ownerIdsByVehicleId[x.VehicleId])
            .ToDictionary(x => x.Key, x => x.Sum(v => v.Amount));

        return new OwnerDataContext(
            owners,
            vehicleCountsByOwnerId,
            activeVehicleCountsByOwnerId,
            bookingsByOwnerId,
            activeRentalCountsByOwnerId,
            grossRevenueByOwnerId);
    }

    private static OwnerDto Map(Owner owner, OwnerDataContext data)
    {
        var grossRevenue = data.GrossRevenueByOwnerId.GetValueOrDefault(owner.Id);
        var partnerShare = grossRevenue * (owner.RevenueSharePercentage / 100m);
        var ownerBookings = data.BookingsByOwnerId.GetValueOrDefault(owner.Id) ?? [];

        return new OwnerDto(
            owner.Id,
            owner.DisplayName,
            owner.ContactName,
            owner.Email,
            owner.Phone,
            owner.RevenueSharePercentage,
            owner.IsActive,
            data.VehicleCountsByOwnerId.GetValueOrDefault(owner.Id),
            data.ActiveRentalCountsByOwnerId.GetValueOrDefault(owner.Id),
            ownerBookings.Count(b => b.Status == BookingStatus.Completed),
            grossRevenue,
            partnerShare,
            grossRevenue - partnerShare,
            owner.CreatedAtUtc,
            owner.UpdatedAtUtc);
    }

    private static void Validate(string displayName, string contactName, string email, string phone, decimal revenueSharePercentage)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Owner name is required.");
        }

        if (string.IsNullOrWhiteSpace(contactName))
        {
            throw new InvalidOperationException("Contact name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Phone is required.");
        }

        if (revenueSharePercentage < 0 || revenueSharePercentage > 100)
        {
            throw new InvalidOperationException("Revenue share percentage must be between 0 and 100.");
        }
    }

    private static void EnsureUnique(IReadOnlyCollection<Owner> owners, string displayName, string email, Guid? currentId)
    {
        if (owners.Any(x => x.Id != currentId && x.DisplayName.Equals(displayName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Owner name already exists.");
        }

        if (owners.Any(x => x.Id != currentId && x.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Owner email already exists.");
        }
    }

    private sealed record OwnerDataContext(
        IReadOnlyCollection<Owner> Owners,
        IReadOnlyDictionary<Guid, int> VehicleCountsByOwnerId,
        IReadOnlyDictionary<Guid, int> ActiveVehicleCountsByOwnerId,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Booking>> BookingsByOwnerId,
        IReadOnlyDictionary<Guid, int> ActiveRentalCountsByOwnerId,
        IReadOnlyDictionary<Guid, decimal> GrossRevenueByOwnerId);
}
