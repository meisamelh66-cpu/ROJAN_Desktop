using System.Security.Cryptography;
using Rojan.Server.Application.Authentication;

namespace Rojan.Server.Infrastructure.Security;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Default
/// <see cref="IPasswordHasher"/> - PBKDF2-HMACSHA256 with a random
/// 128-bit salt per password and <see cref="Iterations"/> matching OWASP's
/// 2023 PBKDF2 guidance, using only <see cref="Rfc2898DeriveBytes"/> from
/// the base class library (no extra NuGet package needed for this, unlike
/// JWT - see <c>JwtTokenService</c>'s own doc comment). The stored format
/// (<c>{iterations}.{saltBase64}.{subkeyBase64}</c>) embeds the iteration
/// count used, so a future increase to <see cref="Iterations"/> does not
/// invalidate already-hashed passwords - <see cref="Verify"/> always uses
/// whatever count is embedded in the hash being checked, not the current
/// constant. Comparison is constant-time
/// (<see cref="CryptographicOperations.FixedTimeEquals"/>) to avoid a
/// timing side-channel revealing how many leading bytes of a guessed hash
/// were correct.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int SubkeySizeBytes = 32;
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, SubkeySizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedSubkey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
