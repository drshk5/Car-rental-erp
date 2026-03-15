using System.Security.Cryptography;

namespace CarRentalERP.Application.Auth;

public static class PasswordSecurity
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "pbkdf2-sha256";

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join('.', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool VerifyPassword(string password, string storedHash, out bool needsUpgrade)
    {
        needsUpgrade = false;

        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var segments = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 4 || !segments[0].Equals(Prefix, StringComparison.Ordinal))
        {
            needsUpgrade = true;
            return storedHash == password;
        }

        if (!int.TryParse(segments[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(segments[2]);
        var expectedHash = Convert.FromBase64String(segments[3]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        needsUpgrade = iterations < Iterations;
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
