using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Maintenance;

public sealed record MaintenanceDto(
    Guid Id,
    Guid VehicleId,
    string VehicleLabel,
    Guid BranchId,
    string BranchName,
    Guid OwnerId,
    string OwnerName,
    string ServiceType,
    DateTime ScheduledAtUtc,
    DateTime? CompletedAtUtc,
    string VendorName,
    decimal Cost,
    MaintenanceStatus Status,
    string Notes,
    bool HasUpcomingBookings,
    DateTime? NextBookingAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record MaintenanceDetailDto(
    MaintenanceDto Record,
    MaintenanceScheduleDto Schedule,
    MaintenanceVehicleOpsDto VehicleOperations);

public sealed record MaintenanceScheduleDto(
    DateTime ScheduledAtUtc,
    DateTime? CompletedAtUtc,
    bool IsOverdue,
    string TimelineLabel);

public sealed record MaintenanceVehicleOpsDto(
    bool HasUpcomingBookings,
    DateTime? NextBookingAtUtc,
    VehicleStatus VehicleStatus);

public sealed record MaintenanceListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? VehicleId = null,
    Guid? BranchId = null,
    Guid? OwnerId = null,
    MaintenanceStatus? Status = null,
    DateTime? ScheduledFromUtc = null,
    DateTime? ScheduledToUtc = null,
    string? Search = null) : PagedRequest(Page, PageSize);

public sealed record CreateMaintenanceRequest(
    Guid VehicleId,
    string ServiceType,
    DateTime ScheduledAtUtc,
    string VendorName,
    decimal Cost,
    string Notes);
