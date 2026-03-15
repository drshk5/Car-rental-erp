using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarRentalERP.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarRentalERP.Api.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtAuthOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _refreshValidationParameters;

    public JwtTokenService(IOptions<JwtAuthOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < JwtAuthOptions.MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Authentication signing key is not configured. Set {JwtAuthOptions.SectionName}__SigningKey with at least {JwtAuthOptions.MinimumSigningKeyLength} characters.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _refreshValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public TokenPair CreateTokenPair(UserProfileDto user)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var identityClaims = BuildIdentityClaims(user);
        var accessToken = WriteToken(identityClaims.Append(new Claim("token_type", "access")), accessExpiresAt);
        var refreshToken = WriteToken(identityClaims.Append(new Claim("token_type", "refresh")), now.AddDays(_options.RefreshTokenDays));

        return new TokenPair(accessToken, refreshToken, accessExpiresAt);
    }

    public TokenPrincipal? ReadRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(refreshToken, _refreshValidationParameters, out _);
            if (!string.Equals(principal.FindFirstValue("token_type"), "refresh", StringComparison.Ordinal))
            {
                return null;
            }

            var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? principal.FindFirstValue(ClaimTypes.Email);

            return Guid.TryParse(userIdClaim, out var userId) && !string.IsNullOrWhiteSpace(email)
                ? new TokenPrincipal(userId, email)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<Claim> BuildIdentityClaims(UserProfileDto user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new("name", user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
            new("branch_id", user.BranchId.ToString())
        };

        foreach (var permission in user.Permissions.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("permission", permission));
        }

        return claims;
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expiresAtUtc)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
