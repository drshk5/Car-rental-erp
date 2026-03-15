using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Users;

public sealed record UserDto(
    Guid Id,
    Guid RoleId,
    string RoleName,
    UserRoleType RoleType,
    Guid BranchId,
    string BranchName,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Permissions,
    bool CanManageUsersAndRoles,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UserDetailDto(
    UserDto Profile,
    UserAccessSummaryDto Access);

public sealed record UserAccessSummaryDto(
    string RoleName,
    UserRoleType RoleType,
    string BranchName,
    IReadOnlyCollection<string> Permissions,
    bool CanManageUsersAndRoles);

public sealed record UserListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? BranchId = null,
    Guid? RoleId = null,
    bool? IsActive = null,
    bool? CanManageUsersAndRoles = null,
    string? Search = null) : PagedRequest(Page, PageSize);

public sealed record CreateUserRequest(
    Guid RoleId,
    Guid BranchId,
    string FullName,
    string Email,
    string Password);

public sealed record UpdateUserRequest(
    Guid RoleId,
    Guid BranchId,
    string FullName,
    string Email);

public sealed record SetUserStatusRequest(bool IsActive);

public sealed record ResetUserPasswordRequest(string Password);
