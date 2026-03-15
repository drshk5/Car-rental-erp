using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Vehicles;

public sealed class VehicleService
{
    private static readonly VehicleStatus[] BlockedAvailabilityStatuses =
    [
        VehicleStatus.ActiveRental,
        VehicleStatus.Maintenance,
        VehicleStatus.OutOfService
    ];

    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<Owner> _ownerRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<MaintenanceRecord> _maintenanceRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(
        IRepository<Vehicle> vehicleRepository,
        IRepository<Branch> branchRepository,
        IRepository<Owner> ownerRepository,
        IRepository<Booking> bookingRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<MaintenanceRecord> maintenanceRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _branchRepository = branchRepository;
        _ownerRepository = ownerRepository;
        _bookingRepository = bookingRepository;
        _rentalRepository = rentalRepository;
        _maintenanceRepository = maintenanceRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(VehicleListRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var normalizedSearch = NormalizeSearch(request.Search);

        var candidates = (await _vehicleRepository.QueryAsync(query =>
            ApplyVehicleFilters(query, request, normalizedSearch)
                .Select(vehicle => new VehicleListCandidate(vehicle.Id, vehicle.PlateNumber)), cancellationToken))
            .ToArray();

        if (candidates.Length == 0)
        {
            return new PagedResult<VehicleDto>(Array.Empty<VehicleDto>(), 0, page, pageSize);
        }

        var candidateVehicleIds = candidates.Select(x => x.Id).ToArray();
        var statusContext = await LoadVehicleStatusContextAsync(candidateVehicleIds, cancellationToken);

        var filteredVehicleIds = candidates
            .Where(x => request.HasActiveRental is null || statusContext.ActiveRentalVehicleIds.Contains(x.Id) == request.HasActiveRental)
            .Where(x => request.HasActiveMaintenance is null || statusContext.ActiveMaintenanceByVehicleId.ContainsKey(x.Id) == request.HasActiveMaintenance)
            .OrderByDescending(x => statusContext.ActiveRentalVehicleIds.Contains(x.Id))
            .ThenByDescending(x => statusContext.UpcomingBookingAtByVehicleId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.PlateNumber)
            .Select(x => x.Id)
            .ToArray();

        var pageVehicleIds = filteredVehicleIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        var data = await LoadContextForVehicleIdsAsync(pageVehicleIds, cancellationToken);
        var result = pageVehicleIds
            .Select(id => data.VehiclesById.GetValueOrDefault(id))
            .Where(vehicle => vehicle is not null)
            .Select(vehicle => Map(vehicle!, data))
            .ToArray();

        return new PagedResult<VehicleDto>(result, filteredVehicleIds.Length, page, pageSize);
    }

    public async Task<VehicleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var data = await LoadContextForVehicleIdsAsync([id], cancellationToken);
        var dto = Map(vehicle, data);
        return new VehicleDetailDto(
            dto,
            new VehicleCommercialsDto(vehicle.DailyRate, vehicle.HourlyRate, vehicle.KmRate, dto.GrossRevenue),
            new VehicleOperationsDto(
                dto.TotalBookings,
                dto.ActiveBookings,
                dto.CompletedRentals,
                dto.NextBookingAtUtc,
                dto.HasActiveRental,
                dto.HasActiveMaintenance));
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.PlateNumber, request.Vin, request.Brand, request.Model, request.Year, request.DailyRate, request.HourlyRate, request.KmRate);

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");
        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Cannot assign vehicle to an inactive branch.");
        }

        var owner = await _ownerRepository.GetByIdAsync(request.OwnerId, cancellationToken)
            ?? throw new InvalidOperationException("Owner not found.");
        if (!owner.IsActive)
        {
            throw new InvalidOperationException("Cannot assign vehicle to an inactive owner.");
        }

        var normalizedPlate = request.PlateNumber.Trim().ToUpperInvariant();
        var normalizedVin = request.Vin.Trim().ToUpperInvariant();
        var vehicles = await _vehicleRepository.QueryAsync(query =>
            query.Where(x => x.PlateNumber == normalizedPlate || x.Vin == normalizedVin), cancellationToken);
        EnsureUniqueness(vehicles, request.PlateNumber, request.Vin, null);
        EnsureAllowedManualStatus(request.Status);

        var vehicle = new Vehicle
        {
            BranchId = request.BranchId,
            OwnerId = request.OwnerId,
            PlateNumber = normalizedPlate,
            Vin = normalizedVin,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            DailyRate = request.DailyRate,
            HourlyRate = request.HourlyRate,
            KmRate = request.KmRate,
            Status = request.Status
        };

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(vehicle.Id, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle could not be reloaded.")).Vehicle;
    }

    public async Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.PlateNumber, request.Vin, request.Brand, request.Model, request.Year, request.DailyRate, request.HourlyRate, request.KmRate);

        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");
        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Cannot assign vehicle to an inactive branch.");
        }

        var owner = await _ownerRepository.GetByIdAsync(request.OwnerId, cancellationToken)
            ?? throw new InvalidOperationException("Owner not found.");
        if (!owner.IsActive)
        {
            throw new InvalidOperationException("Cannot assign vehicle to an inactive owner.");
        }

        var normalizedPlate = request.PlateNumber.Trim().ToUpperInvariant();
        var normalizedVin = request.Vin.Trim().ToUpperInvariant();
        var vehicles = await _vehicleRepository.QueryAsync(query =>
            query.Where(x =>
                x.Id != id &&
                (x.PlateNumber == normalizedPlate || x.Vin == normalizedVin)), cancellationToken);
        EnsureUniqueness(vehicles, request.PlateNumber, request.Vin, id);

        if (!IsAllowedTransition(vehicle.Status, request.Status))
        {
            throw new InvalidOperationException($"Invalid vehicle status transition from {vehicle.Status} to {request.Status}.");
        }

        vehicle.BranchId = request.BranchId;
        vehicle.OwnerId = request.OwnerId;
        vehicle.PlateNumber = normalizedPlate;
        vehicle.Vin = normalizedVin;
        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.DailyRate = request.DailyRate;
        vehicle.HourlyRate = request.HourlyRate;
        vehicle.KmRate = request.KmRate;
        vehicle.Status = request.Status;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(vehicle.Id, cancellationToken))?.Vehicle;
    }

    public async Task<VehicleDto?> SetStatusAsync(Guid id, SetVehicleStatusRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var data = await LoadContextForVehicleIdsAsync([id], cancellationToken);

        if (HasActiveRental(id, data.ActiveRentalByVehicleId) && request.Status != VehicleStatus.ActiveRental)
        {
            throw new InvalidOperationException("Vehicle with active rental cannot be moved manually.");
        }

        if (HasActiveMaintenance(id, data.ActiveMaintenanceByVehicleId) && request.Status != VehicleStatus.Maintenance)
        {
            throw new InvalidOperationException("Vehicle with active maintenance cannot leave maintenance manually.");
        }

        if (!IsAllowedTransition(vehicle.Status, request.Status))
        {
            throw new InvalidOperationException($"Invalid vehicle status transition from {vehicle.Status} to {request.Status}.");
        }

        vehicle.Status = request.Status;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(vehicle.Id, cancellationToken))?.Vehicle;
    }

    public async Task<IReadOnlyCollection<VehicleDto>> SearchAvailableAsync(VehicleAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndAtUtc <= request.StartAtUtc)
        {
            throw new InvalidOperationException("End date must be after start date.");
        }

        if (request.StartAtUtc.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Availability search cannot start in the past.");
        }

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");
        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Branch is inactive.");
        }

        var candidateVehicles = (await _vehicleRepository.QueryAsync(query =>
            query.Where(x =>
                    x.BranchId == request.BranchId &&
                    !BlockedAvailabilityStatuses.Contains(x.Status))
                .Select(x => new AvailabilityCandidate(x.Id, x.DailyRate, x.PlateNumber)), cancellationToken))
            .ToArray();

        if (candidateVehicles.Length == 0)
        {
            return Array.Empty<VehicleDto>();
        }

        var candidateVehicleIds = candidateVehicles.Select(x => x.Id).ToArray();
        var activeMaintenanceVehicleIds = (await _maintenanceRepository.QueryAsync(query =>
            query.Where(x =>
                    candidateVehicleIds.Contains(x.VehicleId) &&
                    x.Status != MaintenanceStatus.Completed)
                .Select(x => x.VehicleId), cancellationToken))
            .ToHashSet();

        var conflictingBookingVehicleIds = (await _bookingRepository.QueryAsync(query =>
            query.Where(x =>
                    candidateVehicleIds.Contains(x.VehicleId) &&
                    (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Active) &&
                    !(x.EndAtUtc <= request.StartAtUtc || x.StartAtUtc >= request.EndAtUtc))
                .Select(x => x.VehicleId), cancellationToken))
            .ToHashSet();

        var availableVehicleIds = candidateVehicles
            .Where(x => !activeMaintenanceVehicleIds.Contains(x.Id))
            .Where(x => !conflictingBookingVehicleIds.Contains(x.Id))
            .OrderBy(x => x.DailyRate)
            .ThenBy(x => x.PlateNumber)
            .Select(x => x.Id)
            .ToArray();

        var data = await LoadContextForVehicleIdsAsync(availableVehicleIds, cancellationToken);
        return availableVehicleIds
            .Select(id => data.VehiclesById.GetValueOrDefault(id))
            .Where(vehicle => vehicle is not null)
            .Select(vehicle => Map(vehicle!, data))
            .ToArray();
    }

    private async Task<VehicleStatusContext> LoadVehicleStatusContextAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (vehicleIds.Count == 0)
        {
            return new VehicleStatusContext(
                new HashSet<Guid>(),
                new Dictionary<Guid, MaintenanceSnapshot>(),
                new Dictionary<Guid, DateTime>());
        }

        var bookings = (await _bookingRepository.QueryAsync(query =>
            query.Where(x => vehicleIds.Contains(x.VehicleId))
                .Select(x => new VehicleBookingSnapshot(x.Id, x.VehicleId, x.StartAtUtc, x.Status)), cancellationToken))
            .ToArray();

        var bookingIds = bookings.Select(x => x.Id).ToArray();
        var latestRentalByBookingId = (bookingIds.Length == 0
                ? Array.Empty<RentalSnapshot>()
                : (await _rentalRepository.QueryAsync(query =>
                    query.Where(x => bookingIds.Contains(x.BookingId))
                        .Select(x => new RentalSnapshot(
                            x.BookingId,
                            x.Status,
                            x.CheckInAtUtc ?? x.CheckOutAtUtc ?? x.CreatedAtUtc)), cancellationToken))
                    .ToArray())
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.SortAtUtc).First());

        var activeMaintenanceByVehicleId = (await _maintenanceRepository.QueryAsync(query =>
            query.Where(x =>
                    vehicleIds.Contains(x.VehicleId) &&
                    x.Status != MaintenanceStatus.Completed)
                .Select(x => new MaintenanceSnapshot(x.VehicleId, x.ScheduledAtUtc)), cancellationToken))
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(m => m.ScheduledAtUtc).First());

        var activeRentalVehicleIds = bookings
            .Where(x => latestRentalByBookingId.GetValueOrDefault(x.Id)?.Status == RentalStatuses.Active)
            .Select(x => x.VehicleId)
            .ToHashSet();

        var upcomingBookingAtByVehicleId = bookings
            .Where(x => x.Status is BookingStatus.Confirmed or BookingStatus.Active)
            .Where(x => x.StartAtUtc >= DateTime.UtcNow)
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.Min(b => b.StartAtUtc));

        return new VehicleStatusContext(
            activeRentalVehicleIds,
            activeMaintenanceByVehicleId,
            upcomingBookingAtByVehicleId);
    }

    private async Task<VehicleDataContext> LoadContextForVehicleIdsAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (vehicleIds.Count == 0)
        {
            return new VehicleDataContext(
                new Dictionary<Guid, Vehicle>(),
                new Dictionary<Guid, Branch>(),
                new Dictionary<Guid, Owner>(),
                Array.Empty<Booking>(),
                new Dictionary<Guid, IReadOnlyCollection<Booking>>(),
                new Dictionary<Guid, Booking>(),
                new Dictionary<Guid, RentalTransaction>(),
                new Dictionary<Guid, RentalTransaction>(),
                new Dictionary<Guid, MaintenanceRecord>(),
                new Dictionary<Guid, decimal>());
        }

        var vehicles = (await _vehicleRepository.QueryAsync(query =>
            query.Where(x => vehicleIds.Contains(x.Id)), cancellationToken))
            .ToArray();
        var branchIds = vehicles.Select(x => x.BranchId).Distinct().ToArray();
        var ownerIds = vehicles.Select(x => x.OwnerId).Distinct().ToArray();

        var branches = (await _branchRepository.QueryAsync(query =>
            query.Where(x => branchIds.Contains(x.Id)), cancellationToken))
            .ToDictionary(x => x.Id);
        var owners = (await _ownerRepository.QueryAsync(query =>
            query.Where(x => ownerIds.Contains(x.Id)), cancellationToken))
            .ToDictionary(x => x.Id);
        var bookings = (await _bookingRepository.QueryAsync(query =>
            query.Where(x => vehicleIds.Contains(x.VehicleId)), cancellationToken))
            .ToArray();
        var bookingIds = bookings.Select(x => x.Id).ToArray();

        var latestRentalByBookingId = (bookingIds.Length == 0
                ? Array.Empty<RentalTransaction>()
                : (await _rentalRepository.QueryAsync(query =>
                    query.Where(x => bookingIds.Contains(x.BookingId)), cancellationToken))
                    .ToArray())
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.CheckInAtUtc ?? r.CheckOutAtUtc ?? r.CreatedAtUtc).First());

        var activeMaintenanceByVehicleId = (await _maintenanceRepository.QueryAsync(query =>
            query.Where(x =>
                vehicleIds.Contains(x.VehicleId) &&
                x.Status != MaintenanceStatus.Completed), cancellationToken))
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(m => m.ScheduledAtUtc).First());

        var paymentAmountsByBookingId = (bookingIds.Length == 0
                ? Array.Empty<Payment>()
                : (await _paymentRepository.QueryAsync(query =>
                    query.Where(x =>
                        bookingIds.Contains(x.BookingId) &&
                        x.PaymentStatus == PaymentStatus.Paid), cancellationToken))
                    .ToArray())
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));

        var activeRentalByVehicleId = bookings
            .Where(x => latestRentalByBookingId.GetValueOrDefault(x.Id)?.Status == RentalStatuses.Active)
            .ToDictionary(x => x.VehicleId, x => latestRentalByBookingId[x.Id]);

        var bookingsByVehicleId = bookings
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Booking>)x.OrderByDescending(b => b.StartAtUtc).ToArray());

        var upcomingBookingsByVehicleId = bookings
            .Where(x => x.Status is BookingStatus.Confirmed or BookingStatus.Active)
            .Where(x => x.StartAtUtc >= DateTime.UtcNow)
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.OrderBy(b => b.StartAtUtc).First());

        return new VehicleDataContext(
            vehicles.ToDictionary(x => x.Id),
            branches,
            owners,
            bookings,
            bookingsByVehicleId,
            upcomingBookingsByVehicleId,
            latestRentalByBookingId,
            activeRentalByVehicleId,
            activeMaintenanceByVehicleId,
            paymentAmountsByBookingId);
    }

    private static VehicleDto Map(Vehicle vehicle, VehicleDataContext data)
    {
        var branch = data.BranchesById.GetValueOrDefault(vehicle.BranchId)
            ?? throw new InvalidOperationException("Vehicle branch mapping is missing.");
        var owner = data.OwnersById.GetValueOrDefault(vehicle.OwnerId)
            ?? throw new InvalidOperationException("Vehicle owner mapping is missing.");
        var bookings = data.BookingsByVehicleId.GetValueOrDefault(vehicle.Id) ?? [];

        var completedRentals = bookings.Count(b =>
            data.LatestRentalByBookingId.GetValueOrDefault(b.Id)?.Status == RentalStatuses.Completed);
        var grossRevenue = bookings.Sum(b => data.PaymentAmountsByBookingId.GetValueOrDefault(b.Id));

        return new VehicleDto(
            vehicle.Id,
            vehicle.BranchId,
            branch.Name,
            vehicle.OwnerId,
            owner.DisplayName,
            vehicle.PlateNumber,
            vehicle.Vin,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.DailyRate,
            vehicle.HourlyRate,
            vehicle.KmRate,
            vehicle.Status,
            bookings.Count,
            bookings.Count(b => b.Status is BookingStatus.Confirmed or BookingStatus.Active),
            completedRentals,
            grossRevenue,
            GetNextBookingAtUtc(vehicle.Id, data.UpcomingBookingsByVehicleId),
            HasActiveRental(vehicle.Id, data.ActiveRentalByVehicleId),
            HasActiveMaintenance(vehicle.Id, data.ActiveMaintenanceByVehicleId),
            vehicle.CreatedAtUtc,
            vehicle.UpdatedAtUtc);
    }

    private static DateTime? GetNextBookingAtUtc(Guid vehicleId, IReadOnlyDictionary<Guid, Booking> upcomingBookingsByVehicleId) =>
        upcomingBookingsByVehicleId.GetValueOrDefault(vehicleId)?.StartAtUtc;

    private static bool HasActiveRental(Guid vehicleId, IReadOnlyDictionary<Guid, RentalTransaction> activeRentalByVehicleId) =>
        activeRentalByVehicleId.ContainsKey(vehicleId);

    private static bool HasActiveMaintenance(Guid vehicleId, IReadOnlyDictionary<Guid, MaintenanceRecord> activeMaintenanceByVehicleId) =>
        activeMaintenanceByVehicleId.ContainsKey(vehicleId);

    private static IQueryable<Vehicle> ApplyVehicleFilters(
        IQueryable<Vehicle> query,
        VehicleListRequest request,
        string? normalizedSearch)
    {
        query = query
            .Where(x => !request.BranchId.HasValue || x.BranchId == request.BranchId.Value)
            .Where(x => !request.OwnerId.HasValue || x.OwnerId == request.OwnerId.Value)
            .Where(x => !request.Status.HasValue || x.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                x.PlateNumber.ToUpper().Contains(normalizedSearch) ||
                x.Brand.ToUpper().Contains(normalizedSearch) ||
                x.Model.ToUpper().Contains(normalizedSearch) ||
                x.Vin.ToUpper().Contains(normalizedSearch));
        }

        return query;
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToUpperInvariant();

    private static void ValidateRequest(string plateNumber, string vin, string brand, string model, int year, decimal dailyRate, decimal hourlyRate, decimal kmRate)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            throw new InvalidOperationException("Plate number is required.");
        }

        if (string.IsNullOrWhiteSpace(vin))
        {
            throw new InvalidOperationException("VIN is required.");
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new InvalidOperationException("Brand is required.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("Model is required.");
        }

        if (year < 1990 || year > DateTime.UtcNow.Year + 1)
        {
            throw new InvalidOperationException("Vehicle year is out of allowed range.");
        }

        if (dailyRate <= 0)
        {
            throw new InvalidOperationException("Daily rate must be greater than zero.");
        }

        if (hourlyRate <= 0)
        {
            throw new InvalidOperationException("Hourly rate must be greater than zero.");
        }

        if (kmRate < 0)
        {
            throw new InvalidOperationException("KM rate cannot be negative.");
        }
    }

    private static void EnsureUniqueness(IReadOnlyCollection<Vehicle> vehicles, string plateNumber, string vin, Guid? currentId)
    {
        var normalizedPlate = plateNumber.Trim().ToUpperInvariant();
        var normalizedVin = vin.Trim().ToUpperInvariant();

        if (vehicles.Any(x => x.Id != currentId && x.PlateNumber.Equals(normalizedPlate, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Plate number already exists.");
        }

        if (vehicles.Any(x => x.Id != currentId && x.Vin.Equals(normalizedVin, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("VIN already exists.");
        }
    }

    private static void EnsureAllowedManualStatus(VehicleStatus status)
    {
        if (status == VehicleStatus.ActiveRental)
        {
            throw new InvalidOperationException("Vehicle cannot be created directly in active rental state.");
        }
    }

    private static bool IsAllowedTransition(VehicleStatus current, VehicleStatus next)
    {
        if (current == next)
        {
            return true;
        }

        return current switch
        {
            VehicleStatus.Available => next is VehicleStatus.Reserved or VehicleStatus.Maintenance or VehicleStatus.OutOfService,
            VehicleStatus.Reserved => next is VehicleStatus.ActiveRental or VehicleStatus.Available or VehicleStatus.Maintenance,
            VehicleStatus.ActiveRental => next == VehicleStatus.Available,
            VehicleStatus.Maintenance => next == VehicleStatus.Available,
            VehicleStatus.OutOfService => next == VehicleStatus.Available,
            _ => false
        };
    }

    private sealed record VehicleDataContext(
        IReadOnlyDictionary<Guid, Vehicle> VehiclesById,
        IReadOnlyDictionary<Guid, Branch> BranchesById,
        IReadOnlyDictionary<Guid, Owner> OwnersById,
        IReadOnlyCollection<Booking> Bookings,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Booking>> BookingsByVehicleId,
        IReadOnlyDictionary<Guid, Booking> UpcomingBookingsByVehicleId,
        IReadOnlyDictionary<Guid, RentalTransaction> LatestRentalByBookingId,
        IReadOnlyDictionary<Guid, RentalTransaction> ActiveRentalByVehicleId,
        IReadOnlyDictionary<Guid, MaintenanceRecord> ActiveMaintenanceByVehicleId,
        IReadOnlyDictionary<Guid, decimal> PaymentAmountsByBookingId);

    private sealed record VehicleStatusContext(
        IReadOnlySet<Guid> ActiveRentalVehicleIds,
        IReadOnlyDictionary<Guid, MaintenanceSnapshot> ActiveMaintenanceByVehicleId,
        IReadOnlyDictionary<Guid, DateTime> UpcomingBookingAtByVehicleId);

    private sealed record VehicleListCandidate(Guid Id, string PlateNumber);

    private sealed record AvailabilityCandidate(Guid Id, decimal DailyRate, string PlateNumber);

    private sealed record VehicleBookingSnapshot(Guid Id, Guid VehicleId, DateTime StartAtUtc, BookingStatus Status);

    private sealed record RentalSnapshot(Guid BookingId, string Status, DateTime SortAtUtc);

    private sealed record MaintenanceSnapshot(Guid VehicleId, DateTime ScheduledAtUtc);
}
