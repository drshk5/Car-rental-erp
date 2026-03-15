using System.Text.Json;
using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Entities;

namespace CarRentalERP.Application.Roles;

public sealed class RoleService
{
    private const string FullAccessPermission = "*";
    private const string ManageUsersAndRolesPermission = "manage.users.roles";

    private static readonly IReadOnlyDictionary<string, string> PermissionCatalog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [FullAccessPermission] = "FULL ACCESS",
        [ManageUsersAndRolesPermission] = "MANAGE USERS ROLES",
        ["manage.branches"] = "MANAGE BRANCHES",
        ["vehicles.write"] = "VEHICLES WRITE",
        ["vehicles.deactivate"] = "VEHICLES DEACTIVATE",
        ["bookings.write"] = "BOOKINGS WRITE",
        ["bookings.cancel"] = "BOOKINGS CANCEL",
        ["rentals.execute"] = "RENTALS EXECUTE",
        ["payments.record"] = "PAYMENTS RECORD",
        ["payments.refund"] = "PAYMENTS REFUND",
        ["reports.view"] = "REPORTS VIEW",
        ["customers.verify"] = "CUSTOMERS VERIFY"
    };

    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RoleService(
        IRepository<Role> roleRepository,
        IRepository<User> userRepository,
        IRepository<Branch> branchRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetAllAsync(RoleListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        return data.Roles
            .Where(x => request.RoleType is null || x.RoleType == request.RoleType)
            .Where(x => request.IncludesManageUsersAndRoles is null || data.CanManageUsersAndRolesByRoleId.GetValueOrDefault(x.Id) == request.IncludesManageUsersAndRoles)
            .Where(x => request.HasAssignedUsers is null || data.UserCountsByRoleId.GetValueOrDefault(x.Id) > 0 == request.HasAssignedUsers)
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.RoleType.ToString().Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                data.PermissionSummariesByRoleId.GetValueOrDefault(x.Id, string.Empty).Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => data.HasFullAccessByRoleId.GetValueOrDefault(x.Id))
            .ThenByDescending(x => data.CanManageUsersAndRolesByRoleId.GetValueOrDefault(x.Id))
            .ThenByDescending(x => data.ActiveUserCountsByRoleId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.Name)
            .Select(x => Map(x, data))
            .ToArray();
    }

    public async Task<RoleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var role = data.Roles.FirstOrDefault(x => x.Id == id);
        if (role is null)
        {
            return null;
        }

        var profile = Map(role, data);
        var assignedUsers = data.Users
            .Where(x => x.RoleId == role.Id)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.FullName)
            .Select(x => new RoleAssignedUserDto(
                x.Id,
                x.FullName,
                x.Email,
                x.IsActive,
                x.BranchId,
                data.BranchNamesById.GetValueOrDefault(x.BranchId, "Unknown Branch")))
            .ToArray();

        return new RoleDetailDto(
            profile,
            new RoleUsageSummaryDto(
                profile.UserCount,
                profile.ActiveUserCount,
                assignedUsers.Select(x => x.BranchId).Distinct().Count(),
                profile.HasFullAccess,
                profile.CanManageUsersAndRoles),
            assignedUsers);
    }

    public Task<IReadOnlyCollection<PermissionCatalogDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PermissionCatalogDto> permissions = PermissionCatalog
            .OrderBy(x => x.Key == FullAccessPermission ? string.Empty : x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PermissionCatalogDto(x.Key, x.Value))
            .ToArray();

        return Task.FromResult(permissions);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedPermissions = NormalizePermissions(request.Permissions);
        Validate(request.Name, normalizedPermissions);

        var roles = await _roleRepository.ListAsync(cancellationToken);
        EnsureUniqueName(roles, request.Name, null);

        var role = new Role
        {
            Name = request.Name.Trim(),
            RoleType = request.RoleType,
            PermissionsJson = SerializePermissions(normalizedPermissions)
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(role.Id, cancellationToken)
            ?? throw new InvalidOperationException("Role could not be reloaded.")).Profile;
    }

    public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedPermissions = NormalizePermissions(request.Permissions);
        Validate(request.Name, normalizedPermissions);

        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var roles = await _roleRepository.ListAsync(cancellationToken);
        EnsureUniqueName(roles, request.Name, id);

        await EnsureManageUsersGuardrailAsync(role, normalizedPermissions, cancellationToken);

        role.Name = request.Name.Trim();
        role.RoleType = request.RoleType;
        role.PermissionsJson = SerializePermissions(normalizedPermissions);
        role.UpdatedAtUtc = DateTime.UtcNow;

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(role.Id, cancellationToken))?.Profile;
    }

    private async Task EnsureManageUsersGuardrailAsync(Role existingRole, IReadOnlyCollection<string> updatedPermissions, CancellationToken cancellationToken)
    {
        var currentlyAllowsManageUsers = HasPermission(ParsePermissions(existingRole.PermissionsJson), ManageUsersAndRolesPermission);
        var willAllowManageUsers = HasPermission(updatedPermissions, ManageUsersAndRolesPermission);

        if (!currentlyAllowsManageUsers || willAllowManageUsers)
        {
            return;
        }

        var roles = await _roleRepository.ListAsync(cancellationToken);
        var users = await _userRepository.ListAsync(cancellationToken);

        var rolePermissionsById = roles.ToDictionary(
            x => x.Id,
            x => x.Id == existingRole.Id ? updatedPermissions : ParsePermissions(x.PermissionsJson));

        var remainingActiveAdminUsers = users.Count(x =>
            x.IsActive &&
            HasPermission(rolePermissionsById.GetValueOrDefault(x.RoleId, []), ManageUsersAndRolesPermission));

        if (remainingActiveAdminUsers == 0)
        {
            throw new InvalidOperationException("At least one active user must retain manage-users-and-roles permission.");
        }
    }

    private async Task<RoleDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.ListAsync(cancellationToken);
        var users = await _userRepository.ListAsync(cancellationToken);
        var branches = await _branchRepository.ListAsync(cancellationToken);

        var permissionsByRoleId = roles.ToDictionary(x => x.Id, x => ParsePermissions(x.PermissionsJson));
        var hasFullAccessByRoleId = permissionsByRoleId.ToDictionary(x => x.Key, x => HasPermission(x.Value, FullAccessPermission));
        var canManageUsersAndRolesByRoleId = permissionsByRoleId.ToDictionary(x => x.Key, x => HasPermission(x.Value, ManageUsersAndRolesPermission));
        var userCountsByRoleId = users.GroupBy(x => x.RoleId).ToDictionary(x => x.Key, x => x.Count());
        var activeUserCountsByRoleId = users.Where(x => x.IsActive).GroupBy(x => x.RoleId).ToDictionary(x => x.Key, x => x.Count());
        var permissionSummariesByRoleId = permissionsByRoleId.ToDictionary(
            x => x.Key,
            x => string.Join(", ", x.Value));
        var branchNamesById = branches.ToDictionary(x => x.Id, x => x.Name);

        return new RoleDataContext(
            roles,
            users,
            permissionsByRoleId,
            hasFullAccessByRoleId,
            canManageUsersAndRolesByRoleId,
            userCountsByRoleId,
            activeUserCountsByRoleId,
            permissionSummariesByRoleId,
            branchNamesById);
    }

    private static RoleDto Map(Role role, RoleDataContext data)
    {
        var permissions = data.PermissionsByRoleId.GetValueOrDefault(role.Id, []);
        return new RoleDto(
            role.Id,
            role.Name,
            role.RoleType,
            role.PermissionsJson,
            permissions,
            data.HasFullAccessByRoleId.GetValueOrDefault(role.Id),
            data.CanManageUsersAndRolesByRoleId.GetValueOrDefault(role.Id),
            data.UserCountsByRoleId.GetValueOrDefault(role.Id),
            data.ActiveUserCountsByRoleId.GetValueOrDefault(role.Id),
            role.CreatedAtUtc,
            role.UpdatedAtUtc);
    }

    private static void Validate(string name, IReadOnlyCollection<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Role name is required.");
        }

        if (permissions.Count == 0)
        {
            throw new InvalidOperationException("At least one permission is required.");
        }

        var invalidPermissions = permissions
            .Where(x => !PermissionCatalog.ContainsKey(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalidPermissions.Length > 0)
        {
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPermissions)}");
        }
    }

    private static void EnsureUniqueName(IReadOnlyCollection<Role> roles, string name, Guid? currentId)
    {
        if (roles.Any(x => x.Id != currentId && x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Role name already exists.");
        }
    }

    private static IReadOnlyCollection<string> NormalizePermissions(IReadOnlyCollection<string> permissions)
    {
        var normalized = permissions?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (normalized.Contains(FullAccessPermission, StringComparer.OrdinalIgnoreCase))
        {
            return [FullAccessPermission];
        }

        return normalized;
    }

    private static bool HasPermission(IReadOnlyCollection<string> permissions, string permission)
        => permissions.Contains(FullAccessPermission, StringComparer.OrdinalIgnoreCase) ||
           permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    private static string SerializePermissions(IReadOnlyCollection<string> permissions)
        => JsonSerializer.Serialize(permissions);

    private static IReadOnlyCollection<string> ParsePermissions(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return [];
        }

        try
        {
            return NormalizePermissions(JsonSerializer.Deserialize<string[]>(permissionsJson) ?? []);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record RoleDataContext(
        IReadOnlyCollection<Role> Roles,
        IReadOnlyCollection<User> Users,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> PermissionsByRoleId,
        IReadOnlyDictionary<Guid, bool> HasFullAccessByRoleId,
        IReadOnlyDictionary<Guid, bool> CanManageUsersAndRolesByRoleId,
        IReadOnlyDictionary<Guid, int> UserCountsByRoleId,
        IReadOnlyDictionary<Guid, int> ActiveUserCountsByRoleId,
        IReadOnlyDictionary<Guid, string> PermissionSummariesByRoleId,
        IReadOnlyDictionary<Guid, string> BranchNamesById);
}
