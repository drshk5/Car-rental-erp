using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Owners;

public sealed record OwnerDto(
    Guid Id,
    string DisplayName,
    string ContactName,
    string Email,
    string Phone,
    decimal RevenueSharePercentage,
    bool IsActive,
    int VehicleCount,
    int ActiveRentalCount,
    int CompletedBookingCount,
    decimal GrossRevenue,
    decimal PartnerShareAmount,
    decimal CompanyShareAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record OwnerDetailDto(
    OwnerDto Profile,
    OwnerPortfolioDto Portfolio,
    OwnerRevenueSplitDto RevenueSplit);

public sealed record OwnerPortfolioDto(
    int VehicleCount,
    int ActiveRentalCount,
    int CompletedBookingCount,
    int ActiveVehicleCount);

public sealed record OwnerRevenueSplitDto(
    decimal GrossRevenue,
    decimal PartnerShareAmount,
    decimal CompanyShareAmount,
    decimal RevenueSharePercentage);

public sealed record OwnerListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    bool? HasVehicles = null) : PagedRequest(Page, PageSize);

public sealed record CreateOwnerRequest(
    string DisplayName,
    string ContactName,
    string Email,
    string Phone,
    decimal RevenueSharePercentage);

public sealed record UpdateOwnerRequest(
    string DisplayName,
    string ContactName,
    string Email,
    string Phone,
    decimal RevenueSharePercentage);

public sealed record SetOwnerStatusRequest(bool IsActive);

public sealed record OwnerRevenueDto(
    Guid OwnerId,
    string OwnerName,
    int VehicleCount,
    int ActiveRentalCount,
    int CompletedBookingCount,
    decimal GrossRevenue,
    decimal PartnerShareAmount,
    decimal CompanyShareAmount);
