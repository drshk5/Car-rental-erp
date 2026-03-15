using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Vehicles;

public sealed record VehicleDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid OwnerId,
    string OwnerName,
    string PlateNumber,
    string Vin,
    string Brand,
    string Model,
    int Year,
    decimal DailyRate,
    decimal HourlyRate,
    decimal KmRate,
    VehicleStatus Status,
    int TotalBookings,
    int ActiveBookings,
    int CompletedRentals,
    decimal GrossRevenue,
    DateTime? NextBookingAtUtc,
    bool HasActiveRental,
    bool HasActiveMaintenance,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record VehicleDetailDto(
    VehicleDto Vehicle,
    VehicleCommercialsDto Commercials,
    VehicleOperationsDto Operations);

public sealed record VehicleCommercialsDto(
    decimal DailyRate,
    decimal HourlyRate,
    decimal KmRate,
    decimal GrossRevenue);

public sealed record VehicleOperationsDto(
    int TotalBookings,
    int ActiveBookings,
    int CompletedRentals,
    DateTime? NextBookingAtUtc,
    bool HasActiveRental,
    bool HasActiveMaintenance);

public sealed record VehicleListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? BranchId = null,
    Guid? OwnerId = null,
    VehicleStatus? Status = null,
    bool? HasActiveRental = null,
    bool? HasActiveMaintenance = null,
    string? Search = null) : PagedRequest(Page, PageSize);

public sealed record CreateVehicleRequest(
    Guid BranchId,
    Guid OwnerId,
    string PlateNumber,
    string Vin,
    string Brand,
    string Model,
    int Year,
    decimal DailyRate,
    decimal HourlyRate,
    decimal KmRate,
    VehicleStatus Status);

public sealed record UpdateVehicleRequest(
    Guid BranchId,
    Guid OwnerId,
    string PlateNumber,
    string Vin,
    string Brand,
    string Model,
    int Year,
    decimal DailyRate,
    decimal HourlyRate,
    decimal KmRate,
    VehicleStatus Status);

public sealed record SetVehicleStatusRequest(VehicleStatus Status);

public sealed record VehicleAvailabilityRequest(
    Guid BranchId,
    DateTime StartAtUtc,
    DateTime EndAtUtc);
