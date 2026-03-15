namespace CarRentalERP.Api.Auth;

public sealed class JwtAuthOptions
{
    public const string SectionName = "Authentication";
    public const int MinimumSigningKeyLength = 32;

    public string Issuer { get; set; } = "CarRentalERP";
    public string Audience { get; set; } = "CarRentalERP.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 480;
    public int RefreshTokenDays { get; set; } = 14;
    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000"];
}
