using System.Text.Json;
using CarRentalERP.Application.Abstractions;
using CarRentalERP.Application.Auth;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Users;

public sealed class UserService
{
    private const string ManageUsersAndRolesPermission = "manage.users.roles";

    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<Branch> branchRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(UserListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var filtered = data.Users
            .Where(x => request.BranchId is null || x.BranchId == request.BranchId)
            .Where(x => request.RoleId is null || x.RoleId == request.RoleId)
            .Where(x => request.IsActive is null || x.IsActive == request.IsActive)
            .Where(x => request.CanManageUsersAndRoles is null || data.CanManageUsersAndRolesByUserId.GetValueOrDefault(x.Id) == request.CanManageUsersAndRoles)
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.FullName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                x.Email.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                data.RoleNamesById.GetValueOrDefault(x.RoleId, string.Empty).Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                data.BranchNamesById.GetValueOrDefault(x.BranchId, string.Empty).Contains(request.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => data.CanManageUsersAndRolesByUserId.GetValueOrDefault(x.Id))
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.Email)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        return new PagedResult<UserDto>(items, filtered.Length, page, pageSize);
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var user = data.Users.FirstOrDefault(x => x.Id == id);
        if (user is null)
        {
            return null;
        }

        var profile = Map(user, data);
        return new UserDetailDto(
            profile,
            new UserAccessSummaryDto(
                profile.RoleName,
                profile.RoleType,
                profile.BranchName,
                profile.Permissions,
                profile.CanManageUsersAndRoles));
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);
        var normalizedEmail = NormalizeEmail(request.Email);

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");

        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Cannot assign user to an inactive branch.");
        }

        var existingUsers = await _userRepository.ListAsync(cancellationToken);
        if (existingUsers.Any(x => x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            RoleId = role.Id,
            BranchId = branch.Id,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordSecurity.HashPassword(request.Password),
            IsActive = true
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("User could not be reloaded.")).Profile;
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var branch = await _branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");

        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Cannot assign user to an inactive branch.");
        }

        var existingUsers = await _userRepository.ListAsync(cancellationToken);
        if (existingUsers.Any(x => x.Id != id && x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        user.RoleId = role.Id;
        user.BranchId = branch.Id;
        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken))?.Profile;
    }

    public async Task<UserDto?> SetStatusAsync(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (!request.IsActive && user.IsActive)
        {
            await EnsureNotLastManageUsersAdminAsync(id, cancellationToken);
        }

        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken))?.Profile;
    }

    public async Task<UserDto?> ResetPasswordAsync(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.Password);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.PasswordHash = PasswordSecurity.HashPassword(request.Password);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(user.Id, cancellationToken))?.Profile;
    }

    private async Task EnsureNotLastManageUsersAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var data = await LoadContextAsync(cancellationToken);
        if (!data.CanManageUsersAndRolesByUserId.GetValueOrDefault(userId))
        {
            return;
        }

        var remainingActiveAdmins = data.Users.Count(x => x.IsActive && x.Id != userId && data.CanManageUsersAndRolesByUserId.GetValueOrDefault(x.Id));
        if (remainingActiveAdmins == 0)
        {
            throw new InvalidOperationException("The last active user with manage-users-and-roles permission cannot be deactivated.");
        }
    }

    private async Task<UserDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAsync(cancellationToken);
        var roles = await _roleRepository.ListAsync(cancellationToken);
        var branches = await _branchRepository.ListAsync(cancellationToken);

        var rolesById = roles.ToDictionary(x => x.Id);
        var branchesById = branches.ToDictionary(x => x.Id);
        var roleNamesById = roles.ToDictionary(x => x.Id, x => x.Name);
        var branchNamesById = branches.ToDictionary(x => x.Id, x => x.Name);
        var permissionsByRoleId = roles.ToDictionary(x => x.Id, x => ParsePermissions(x.PermissionsJson));
        var canManageUsersAndRolesByUserId = users.ToDictionary(
            x => x.Id,
            x => permissionsByRoleId.GetValueOrDefault(x.RoleId, []).Contains(ManageUsersAndRolesPermission, StringComparer.OrdinalIgnoreCase));

        return new UserDataContext(
            users,
            rolesById,
            branchesById,
            roleNamesById,
            branchNamesById,
            permissionsByRoleId,
            canManageUsersAndRolesByUserId);
    }

    private static UserDto Map(User user, UserDataContext data)
    {
        var role = data.RolesById.TryGetValue(user.RoleId, out var foundRole)
            ? foundRole
            : throw new InvalidOperationException("User role mapping is missing.");

        var branch = data.BranchesById.TryGetValue(user.BranchId, out var foundBranch)
            ? foundBranch
            : throw new InvalidOperationException("User branch mapping is missing.");

        var permissions = data.PermissionsByRoleId.GetValueOrDefault(user.RoleId, []);
        return new UserDto(
            user.Id,
            user.RoleId,
            role.Name,
            role.RoleType,
            user.BranchId,
            branch.Name,
            user.FullName,
            user.Email,
            user.IsActive,
            permissions,
            data.CanManageUsersAndRolesByUserId.GetValueOrDefault(user.Id),
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }

    private static void ValidateCreateRequest(CreateUserRequest request)
    {
        ValidateNameAndEmail(request.FullName, request.Email);
        ValidatePassword(request.Password);
    }

    private static void ValidateUpdateRequest(UpdateUserRequest request)
    {
        ValidateNameAndEmail(request.FullName, request.Email);
    }

    private static void ValidateNameAndEmail(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email is invalid.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters.");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static IReadOnlyCollection<string> ParsePermissions(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return [];
        }

        try
        {
            return (JsonSerializer.Deserialize<string[]>(permissionsJson) ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record UserDataContext(
        IReadOnlyCollection<User> Users,
        IReadOnlyDictionary<Guid, Role> RolesById,
        IReadOnlyDictionary<Guid, Branch> BranchesById,
        IReadOnlyDictionary<Guid, string> RoleNamesById,
        IReadOnlyDictionary<Guid, string> BranchNamesById,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> PermissionsByRoleId,
        IReadOnlyDictionary<Guid, bool> CanManageUsersAndRolesByUserId);
}
