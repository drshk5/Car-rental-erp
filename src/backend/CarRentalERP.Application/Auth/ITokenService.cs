namespace CarRentalERP.Application.Auth;

public interface ITokenService
{
    TokenPair CreateTokenPair(UserProfileDto user);
    TokenPrincipal? ReadRefreshToken(string refreshToken);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

public sealed record TokenPrincipal(Guid UserId, string Email);
