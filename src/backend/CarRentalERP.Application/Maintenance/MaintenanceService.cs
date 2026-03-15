using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Maintenance;

public sealed class MaintenanceService
{
    private readonly IRepository<MaintenanceRecord> _maintenanceRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<Owner> _ownerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MaintenanceService(
        IRepository<MaintenanceRecord> maintenanceRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Booking> bookingRepository,
        IRepository<Branch> branchRepository,
        IRepository<Owner> ownerRepository,
        IUnitOfWork unitOfWork)
    {
        _maintenanceRepository = maintenanceRepository;
        _vehicleRepository = vehicleRepository;
        _bookingRepository = bookingRepository;
        _branchRepository = branchRepository;
        _ownerRepository = ownerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<MaintenanceDto>> GetPagedAsync(MaintenanceListRequest request, CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request);

        var data = await LoadContextAsync(cancellationToken);
        var filtered = data.Records
            .Where(x => request.VehicleId is null || x.VehicleId == request.VehicleId)
            .Where(x => request.BranchId is null || data.VehiclesById[x.VehicleId].BranchId == request.BranchId)
            .Where(x => request.OwnerId is null || data.VehiclesById[x.VehicleId].OwnerId == request.OwnerId)
            .Where(x => request.Status is null || x.Status == request.Status)
            .Where(x => request.ScheduledFromUtc is null || x.ScheduledAtUtc >= request.ScheduledFromUtc)
            .Where(x => request.ScheduledToUtc is null || x.ScheduledAtUtc <= request.ScheduledToUtc)
            .Where(x => MatchesSearch(x, request.Search, data))
            .OrderByDescending(x => x.Status == MaintenanceStatus.InProgress)
            .ThenBy(x => x.Status == MaintenanceStatus.Scheduled ? x.ScheduledAtUtc : DateTime.MaxValue)
            .ThenByDescending(x => x.ScheduledAtUtc)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var result = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        return new PagedResult<MaintenanceDto>(result, filtered.Length, page, pageSize);
    }

    public async Task<IReadOnlyCollection<MaintenanceDto>> GetByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        if (!data.VehiclesById.ContainsKey(vehicleId))
        {
            throw new InvalidOperationException("Vehicle not found.");
        }

        return data.Records
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.ScheduledAtUtc)
            .Select(x => Map(x, data))
            .ToArray();
    }

    public async Task<MaintenanceDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var record = data.Records.FirstOrDefault(x => x.Id == id);
        if (record is null)
        {
            return null;
        }

        var dto = Map(record, data);
        return new MaintenanceDetailDto(
            dto,
            new MaintenanceScheduleDto(
                record.ScheduledAtUtc,
                record.CompletedAtUtc,
                IsOverdue(record),
                BuildTimelineLabel(record)),
            new MaintenanceVehicleOpsDto(
                dto.HasUpcomingBookings,
                dto.NextBookingAtUtc,
                data.VehiclesById[record.VehicleId].Status));
    }

    public async Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var data = await LoadContextAsync(cancellationToken);
        var vehicle = data.VehiclesById.GetValueOrDefault(request.VehicleId)
            ?? throw new InvalidOperationException("Vehicle not found.");

        if (vehicle.Status is VehicleStatus.ActiveRental or VehicleStatus.OutOfService)
        {
            throw new InvalidOperationException("Vehicle is not eligible for maintenance scheduling.");
        }

        var upcomingBooking = data.UpcomingBookingsByVehicleId.GetValueOrDefault(vehicle.Id);
        if (upcomingBooking is not null && upcomingBooking.StartAtUtc <= request.ScheduledAtUtc)
        {
            throw new InvalidOperationException("Vehicle has upcoming bookings and cannot be moved to maintenance.");
        }

        var activeMaintenance = data.ActiveMaintenanceByVehicleId.ContainsKey(vehicle.Id);
        if (activeMaintenance)
        {
            throw new InvalidOperationException("Vehicle already has an active maintenance record.");
        }

        var scheduledAtUtc = request.ScheduledAtUtc.ToUniversalTime();
        var record = new MaintenanceRecord
        {
            VehicleId = vehicle.Id,
            ServiceType = request.ServiceType.Trim(),
            ScheduledAtUtc = scheduledAtUtc,
            VendorName = request.VendorName.Trim(),
            Cost = request.Cost,
            Notes = request.Notes.Trim(),
            Status = scheduledAtUtc <= DateTime.UtcNow
                ? MaintenanceStatus.InProgress
                : MaintenanceStatus.Scheduled
        };

        vehicle.Status = VehicleStatus.Maintenance;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _maintenanceRepository.AddAsync(record, cancellationToken);
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(record.Id, cancellationToken)
            ?? throw new InvalidOperationException("Maintenance record could not be reloaded.")).Record;
    }

    public async Task<MaintenanceDto?> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _maintenanceRepository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Status == MaintenanceStatus.Completed)
        {
            throw new InvalidOperationException("Maintenance is already completed.");
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(record.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var hasUpcomingBooking = bookings.Any(x =>
            x.VehicleId == vehicle.Id &&
            x.Status is BookingStatus.Confirmed or BookingStatus.Active &&
            x.EndAtUtc > DateTime.UtcNow);

        record.Status = MaintenanceStatus.Completed;
        record.CompletedAtUtc = DateTime.UtcNow;
        record.UpdatedAtUtc = DateTime.UtcNow;

        vehicle.Status = hasUpcomingBooking ? VehicleStatus.Reserved : VehicleStatus.Available;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await _maintenanceRepository.UpdateAsync(record, cancellationToken);
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(record.Id, cancellationToken))?.Record;
    }

    private async Task<MaintenanceDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var records = await _maintenanceRepository.ListAsync(cancellationToken);
        var vehicles = (await _vehicleRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var branches = (await _branchRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var owners = (await _ownerRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);

        var upcomingBookingsByVehicleId = bookings
            .Where(x => x.Status is BookingStatus.Confirmed or BookingStatus.Active)
            .Where(x => x.StartAtUtc >= DateTime.UtcNow)
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.OrderBy(b => b.StartAtUtc).First());

        var activeMaintenanceByVehicleId = records
            .Where(x => x.Status != MaintenanceStatus.Completed)
            .GroupBy(x => x.VehicleId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(m => m.ScheduledAtUtc).First());

        return new MaintenanceDataContext(
            records,
            vehicles,
            branches,
            owners,
            upcomingBookingsByVehicleId,
            activeMaintenanceByVehicleId);
    }

    private static MaintenanceDto Map(MaintenanceRecord record, MaintenanceDataContext data)
    {
        var vehicle = data.VehiclesById.GetValueOrDefault(record.VehicleId)
            ?? throw new InvalidOperationException("Maintenance vehicle mapping is missing.");
        var branch = data.BranchesById.GetValueOrDefault(vehicle.BranchId)
            ?? throw new InvalidOperationException("Maintenance branch mapping is missing.");
        var owner = data.OwnersById.GetValueOrDefault(vehicle.OwnerId)
            ?? throw new InvalidOperationException("Maintenance owner mapping is missing.");
        var nextBookingAtUtc = data.UpcomingBookingsByVehicleId.GetValueOrDefault(vehicle.Id)?.StartAtUtc;

        return new MaintenanceDto(
            record.Id,
            record.VehicleId,
            $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
            vehicle.BranchId,
            branch.Name,
            vehicle.OwnerId,
            owner.DisplayName,
            record.ServiceType,
            record.ScheduledAtUtc,
            record.CompletedAtUtc,
            record.VendorName,
            record.Cost,
            record.Status,
            record.Notes,
            nextBookingAtUtc is not null,
            nextBookingAtUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }

    private static bool MatchesSearch(MaintenanceRecord record, string? search, MaintenanceDataContext data)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        var vehicle = data.VehiclesById[record.VehicleId];
        var branch = data.BranchesById[vehicle.BranchId];
        var owner = data.OwnersById[vehicle.OwnerId];

        return vehicle.PlateNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.Model.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               branch.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               owner.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               record.ServiceType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               record.VendorName.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOverdue(MaintenanceRecord record) =>
        record.Status != MaintenanceStatus.Completed && record.ScheduledAtUtc < DateTime.UtcNow;

    private static string BuildTimelineLabel(MaintenanceRecord record) =>
        record.Status switch
        {
            MaintenanceStatus.Completed => "Completed",
            MaintenanceStatus.InProgress when record.ScheduledAtUtc < DateTime.UtcNow => "In progress",
            MaintenanceStatus.Scheduled when record.ScheduledAtUtc > DateTime.UtcNow => "Upcoming",
            _ => "Pending"
        };

    private static void ValidateListRequest(MaintenanceListRequest request)
    {
        if (request.ScheduledFromUtc.HasValue && request.ScheduledToUtc.HasValue && request.ScheduledFromUtc > request.ScheduledToUtc)
        {
            throw new InvalidOperationException("Scheduled from date cannot be after scheduled to date.");
        }
    }

    private static void ValidateCreateRequest(CreateMaintenanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceType))
        {
            throw new InvalidOperationException("Service type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VendorName))
        {
            throw new InvalidOperationException("Vendor name is required.");
        }

        if (request.Cost < 0)
        {
            throw new InvalidOperationException("Cost cannot be negative.");
        }

        if (request.ScheduledAtUtc == default)
        {
            throw new InvalidOperationException("Scheduled date is required.");
        }

        if (request.ScheduledAtUtc.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Maintenance cannot be scheduled in the past.");
        }
    }

    private sealed record MaintenanceDataContext(
        IReadOnlyCollection<MaintenanceRecord> Records,
        IReadOnlyDictionary<Guid, Vehicle> VehiclesById,
        IReadOnlyDictionary<Guid, Branch> BranchesById,
        IReadOnlyDictionary<Guid, Owner> OwnersById,
        IReadOnlyDictionary<Guid, Booking> UpcomingBookingsByVehicleId,
        IReadOnlyDictionary<Guid, MaintenanceRecord> ActiveMaintenanceByVehicleId);
}
