using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Branches;

public sealed record BranchDto(
    Guid Id,
    string Name,
    string City,
    string Address,
    string Phone,
    bool IsActive,
    int UserCount,
    int VehicleCount,
    int ActiveRentalCount,
    int UpcomingBookingCount,
    decimal GrossRevenue,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record BranchDetailDto(
    BranchDto Branch,
    BranchOperationsDto Operations,
    BranchCommercialsDto Commercials);

public sealed record BranchOperationsDto(
    int UserCount,
    int VehicleCount,
    int ActiveRentalCount,
    int UpcomingBookingCount,
    int AvailableVehicleCount);

public sealed record BranchCommercialsDto(
    decimal GrossRevenue);

public sealed record BranchListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    bool? HasVehicles = null) : PagedRequest(Page, PageSize);

public sealed record CreateBranchRequest(
    string Name,
    string City,
    string Address,
    string Phone);

public sealed record UpdateBranchRequest(
    string Name,
    string City,
    string Address,
    string Phone);

public sealed record SetBranchStatusRequest(bool IsActive);
