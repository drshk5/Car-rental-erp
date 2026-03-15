using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Rentals;

public sealed record RentalDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    Guid VehicleId,
    string VehicleLabel,
    string VehiclePlate,
    string VehicleVin,
    Guid PickupBranchId,
    string PickupBranchName,
    Guid ReturnBranchId,
    string ReturnBranchName,
    DateTime BookingStartAtUtc,
    DateTime BookingEndAtUtc,
    DateTime? CheckOutAtUtc,
    DateTime? CheckInAtUtc,
    int OdometerOut,
    int? OdometerIn,
    int DistanceTravelled,
    string FuelOut,
    string? FuelIn,
    decimal ExtraCharges,
    string DamageNotes,
    decimal FinalAmount,
    decimal TotalPaid,
    decimal OutstandingBalance,
    bool IsOverdue,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record RentalDetailDto(
    RentalDto Rental,
    RentalFinancialsDto Financials,
    RentalTimelineDto Timeline);

public sealed record RentalFinancialsDto(
    decimal QuotedTotal,
    decimal ExtraCharges,
    decimal FinalAmount,
    decimal TotalPaid,
    decimal OutstandingBalance);

public sealed record RentalTimelineDto(
    DateTime BookingStartAtUtc,
    DateTime BookingEndAtUtc,
    DateTime? CheckOutAtUtc,
    DateTime? CheckInAtUtc,
    bool IsOverdue);

public sealed record RentalSummaryDto(
    int TotalRentals,
    int ActiveRentals,
    int CompletedRentals,
    int OverdueRentals,
    decimal OutstandingBalance);

public sealed record RentalListResponse(
    PagedResult<RentalDto> Rentals,
    RentalSummaryDto Summary);

public sealed record RentalStatsDto(
    int TotalRentals,
    int ActiveRentals,
    int CompletedRentals,
    int OverdueRentals,
    int TodayCheckouts,
    int TodayCheckins,
    decimal OutstandingBalance,
    decimal RevenueToday,
    decimal RevenueThisWeek,
    decimal RevenueThisMonth,
    decimal AverageRentalDurationDays);

public sealed record RentalDashboardSummaryDto(
    int ActiveRentals,
    int OverdueRentals,
    int TodayPickups,
    int TodayReturns,
    IReadOnlyCollection<RentalDto> UpcomingReturns,
    IReadOnlyCollection<RentalDto> RecentCheckouts);

public sealed record RentalListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    Guid? BranchId = null,
    Guid? CustomerId = null,
    Guid? VehicleId = null,
    bool? IsOverdue = null) : PagedRequest(Page, PageSize);

public sealed record CheckoutRequest(
    Guid BookingId,
    int OdometerOut,
    string FuelOut,
    string Notes);

public sealed record CheckinRequest(
    int OdometerIn,
    string FuelIn,
    decimal ExtraCharges,
    string DamageNotes);

public sealed record UpdateRentalDamageRequest(
    string DamageNotes);
