namespace CarRentalERP.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int AvailableVehicles,
    int ActiveRentals,
    int TodayPickups,
    int TodayReturns,
    int OverdueRentals,
    int UnpaidBookings,
    decimal RevenueToday,
    decimal RevenueThisMonth,
    int VehiclesInMaintenance,
    IReadOnlyCollection<OwnerRevenueSummaryDto> OwnerRevenue);

public sealed record OwnerRevenueSummaryDto(
    Guid OwnerId,
    string OwnerName,
    decimal GrossRevenue,
    decimal PartnerShareAmount,
    decimal CompanyShareAmount,
    int VehicleCount);
