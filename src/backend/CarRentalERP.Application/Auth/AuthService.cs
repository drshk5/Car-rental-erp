using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Entities;
using System.Text.Json;

namespace CarRentalERP.Application.Auth;

public sealed class AuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var normalizedEmail = request.Email.Trim();
        var user = (await _userRepository.ListAsync(cancellationToken))
            .FirstOrDefault(x =>
                x.IsActive &&
                x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return null;
        }

        if (!PasswordSecurity.VerifyPassword(request.Password, user.PasswordHash, out var needsUpgrade))
        {
            return null;
        }

        if (needsUpgrade)
        {
            user.PasswordHash = PasswordSecurity.HashPassword(request.Password);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var principal = _tokenService.ReadRefreshToken(refreshToken);
        if (principal is null)
        {
            return null;
        }

        var user = (await _userRepository.ListAsync(cancellationToken))
            .FirstOrDefault(x =>
                x.Id == principal.UserId &&
                x.IsActive &&
                x.Email.Equals(principal.Email, StringComparison.OrdinalIgnoreCase));

        return user is null ? null : await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = (await _userRepository.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.IsActive && x.Id == userId);

        if (user is null)
        {
            return null;
        }

        var role = await GetRoleAsync(user.RoleId, cancellationToken);
        return new UserProfileDto(user.Id, user.FullName, user.Email, role.RoleType, user.BranchId, ParsePermissions(role.PermissionsJson));
    }

    public async Task<UserProfileDto?> GetProfileAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = (await _userRepository.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.IsActive && x.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return null;
        }

        var role = await GetRoleAsync(user.RoleId, cancellationToken);
        return new UserProfileDto(user.Id, user.FullName, user.Email, role.RoleType, user.BranchId, ParsePermissions(role.PermissionsJson));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(user.RoleId, cancellationToken);
        var profile = new UserProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            role.RoleType,
            user.BranchId,
            ParsePermissions(role.PermissionsJson));
        var tokens = _tokenService.CreateTokenPair(profile);

        return new AuthResponse(
            AccessToken: tokens.AccessToken,
            RefreshToken: tokens.RefreshToken,
            ExpiresAtUtc: tokens.ExpiresAtUtc,
            User: profile);
    }

    private async Task<Role> GetRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role ?? throw new InvalidOperationException("Role not found for authenticated user.");
    }

    private static IReadOnlyCollection<string> ParsePermissions(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(permissionsJson) ?? [];
            return parsed
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
