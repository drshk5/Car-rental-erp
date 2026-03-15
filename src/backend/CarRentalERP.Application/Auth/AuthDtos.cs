using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    UserRoleType Role,
    Guid BranchId,
    IReadOnlyCollection<string> Permissions);
