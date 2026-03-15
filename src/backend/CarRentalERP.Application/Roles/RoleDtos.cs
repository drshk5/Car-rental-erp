using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Application.Roles;

public sealed record RoleDto(
    Guid Id,
    string Name,
    UserRoleType RoleType,
    string PermissionsJson,
    IReadOnlyCollection<string> Permissions,
    bool HasFullAccess,
    bool CanManageUsersAndRoles,
    int UserCount,
    int ActiveUserCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record RoleDetailDto(
    RoleDto Profile,
    RoleUsageSummaryDto Usage,
    IReadOnlyCollection<RoleAssignedUserDto> AssignedUsers);

public sealed record RoleUsageSummaryDto(
    int UserCount,
    int ActiveUserCount,
    int BranchCount,
    bool HasFullAccess,
    bool CanManageUsersAndRoles);

public sealed record RoleAssignedUserDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    Guid BranchId,
    string BranchName);

public sealed record RoleListRequest(
    UserRoleType? RoleType = null,
    bool? IncludesManageUsersAndRoles = null,
    bool? HasAssignedUsers = null,
    string? Search = null);

public sealed record CreateRoleRequest(
    string Name,
    UserRoleType RoleType,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateRoleRequest(
    string Name,
    UserRoleType RoleType,
    IReadOnlyCollection<string> Permissions);

public sealed record PermissionCatalogDto(
    string Key,
    string Label);
