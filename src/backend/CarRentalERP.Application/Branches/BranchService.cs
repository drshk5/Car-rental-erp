using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Branches;

public sealed class BranchService
{
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BranchService(
        IRepository<Branch> branchRepository,
        IRepository<User> userRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Booking> bookingRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _userRepository = userRepository;
        _vehicleRepository = vehicleRepository;
        _bookingRepository = bookingRepository;
        _rentalRepository = rentalRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<BranchDto>> GetPagedAsync(BranchListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);

        var filtered = data.Branches
            .Where(x => request.IsActive is null || x.IsActive == request.IsActive)
            .Where(x => request.HasVehicles is null || data.VehicleCountsByBranchId.GetValueOrDefault(x.Id) > 0 == request.HasVehicles)
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.City.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => data.GrossRevenueByBranchId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.Name)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var pagedData = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        return new PagedResult<BranchDto>(pagedData, filtered.Length, page, pageSize);
    }

    public async Task<BranchDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var branch = data.Branches.FirstOrDefault(x => x.Id == id);
        if (branch is null)
        {
            return null;
        }

        var dto = Map(branch, data);
        return new BranchDetailDto(
            dto,
            new BranchOperationsDto(
                dto.UserCount,
                dto.VehicleCount,
                dto.ActiveRentalCount,
                dto.UpcomingBookingCount,
                data.AvailableVehicleCountsByBranchId.GetValueOrDefault(branch.Id)),
            new BranchCommercialsDto(dto.GrossRevenue));
    }

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.Name, request.City, request.Address, request.Phone);

        var existingBranches = await _branchRepository.ListAsync(cancellationToken);
        EnsureNoDuplicate(existingBranches, request.Name, request.City, null);

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = true
        };

        await _branchRepository.AddAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(branch.Id, cancellationToken)
            ?? throw new InvalidOperationException("Branch could not be reloaded.")).Branch;
    }

    public async Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.Name, request.City, request.Address, request.Phone);

        var branch = await _branchRepository.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        var existingBranches = await _branchRepository.ListAsync(cancellationToken);
        EnsureNoDuplicate(existingBranches, request.Name, request.City, id);

        branch.Name = request.Name.Trim();
        branch.City = request.City.Trim();
        branch.Address = request.Address.Trim();
        branch.Phone = request.Phone.Trim();
        branch.UpdatedAtUtc = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(branch.Id, cancellationToken))?.Branch;
    }

    public async Task<BranchDto?> SetStatusAsync(Guid id, SetBranchStatusRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await _branchRepository.GetByIdAsync(id, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        if (branch.IsActive == request.IsActive)
        {
            return (await GetByIdAsync(branch.Id, cancellationToken))?.Branch;
        }

        if (!request.IsActive)
        {
            var data = await LoadContextAsync(cancellationToken);
            if (data.ActiveRentalCountsByBranchId.GetValueOrDefault(id) > 0)
            {
                throw new InvalidOperationException("Branch with active rentals cannot be deactivated.");
            }

            if (data.UpcomingBookingCountsByBranchId.GetValueOrDefault(id) > 0)
            {
                throw new InvalidOperationException("Branch with upcoming bookings cannot be deactivated.");
            }
        }

        branch.IsActive = request.IsActive;
        branch.UpdatedAtUtc = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(branch.Id, cancellationToken))?.Branch;
    }

    private async Task<BranchDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.ListAsync(cancellationToken);
        var users = await _userRepository.ListAsync(cancellationToken);
        var vehicles = await _vehicleRepository.ListAsync(cancellationToken);
        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var payments = await _paymentRepository.ListAsync(cancellationToken);

        var vehicleCountsByBranchId = vehicles
            .GroupBy(x => x.BranchId)
            .ToDictionary(x => x.Key, x => x.Count());

        var availableVehicleCountsByBranchId = vehicles
            .Where(x => x.Status == VehicleStatus.Available)
            .GroupBy(x => x.BranchId)
            .ToDictionary(x => x.Key, x => x.Count());

        var userCountsByBranchId = users
            .GroupBy(x => x.BranchId)
            .ToDictionary(x => x.Key, x => x.Count());

        var branchIdsByVehicleId = vehicles.ToDictionary(x => x.Id, x => x.BranchId);

        var activeRentalCountsByBranchId = rentals
            .Where(x => x.Status == RentalStatuses.Active)
            .Join(bookings, rental => rental.BookingId, booking => booking.Id, (rental, booking) => booking)
            .Where(booking => branchIdsByVehicleId.ContainsKey(booking.VehicleId))
            .GroupBy(booking => branchIdsByVehicleId[booking.VehicleId])
            .ToDictionary(x => x.Key, x => x.Count());

        var upcomingBookingCountsByBranchId = bookings
            .Where(x => x.Status is BookingStatus.Confirmed or BookingStatus.Active)
            .Where(x => x.StartAtUtc >= DateTime.UtcNow)
            .GroupBy(x => x.PickupBranchId)
            .ToDictionary(x => x.Key, x => x.Count());

        var grossRevenueByBranchId = payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .Join(bookings, payment => payment.BookingId, booking => booking.Id, (payment, booking) => new { payment.Amount, booking.VehicleId })
            .Where(x => branchIdsByVehicleId.ContainsKey(x.VehicleId))
            .GroupBy(x => branchIdsByVehicleId[x.VehicleId])
            .ToDictionary(x => x.Key, x => x.Sum(v => v.Amount));

        return new BranchDataContext(
            branches,
            userCountsByBranchId,
            vehicleCountsByBranchId,
            availableVehicleCountsByBranchId,
            activeRentalCountsByBranchId,
            upcomingBookingCountsByBranchId,
            grossRevenueByBranchId);
    }

    private static BranchDto Map(Branch branch, BranchDataContext data) =>
        new(
            branch.Id,
            branch.Name,
            branch.City,
            branch.Address,
            branch.Phone,
            branch.IsActive,
            data.UserCountsByBranchId.GetValueOrDefault(branch.Id),
            data.VehicleCountsByBranchId.GetValueOrDefault(branch.Id),
            data.ActiveRentalCountsByBranchId.GetValueOrDefault(branch.Id),
            data.UpcomingBookingCountsByBranchId.GetValueOrDefault(branch.Id),
            data.GrossRevenueByBranchId.GetValueOrDefault(branch.Id),
            branch.CreatedAtUtc,
            branch.UpdatedAtUtc);

    private static void ValidateRequest(string name, string city, string address, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Branch name is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new InvalidOperationException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Address is required.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Phone is required.");
        }
    }

    private static void EnsureNoDuplicate(IReadOnlyCollection<Branch> existingBranches, string name, string city, Guid? currentId)
    {
        var normalizedName = name.Trim();
        var normalizedCity = city.Trim();

        var duplicate = existingBranches.Any(x =>
            x.Id != currentId &&
            x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase) &&
            x.City.Equals(normalizedCity, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException("A branch with the same name already exists in this city.");
        }
    }

    private sealed record BranchDataContext(
        IReadOnlyCollection<Branch> Branches,
        IReadOnlyDictionary<Guid, int> UserCountsByBranchId,
        IReadOnlyDictionary<Guid, int> VehicleCountsByBranchId,
        IReadOnlyDictionary<Guid, int> AvailableVehicleCountsByBranchId,
        IReadOnlyDictionary<Guid, int> ActiveRentalCountsByBranchId,
        IReadOnlyDictionary<Guid, int> UpcomingBookingCountsByBranchId,
        IReadOnlyDictionary<Guid, decimal> GrossRevenueByBranchId);
}
