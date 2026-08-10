using System.Security.Cryptography;

namespace InventoryManagementSystem.Utils;

public static class PasswordHasher
{
    public const int SaltSize = 16;
    public const int HashSize = 32;
    public const int Iterations = 210_000;

    public static (byte[] Hash, byte[] Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (hash, salt);
    }

    public static bool VerifyPassword(string password, byte[] expectedHash, byte[] salt)
    {
        if (string.IsNullOrEmpty(password) || expectedHash.Length == 0 || salt.Length == 0)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
