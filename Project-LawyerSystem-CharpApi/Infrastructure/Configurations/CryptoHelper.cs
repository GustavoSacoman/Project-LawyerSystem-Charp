using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Project_LawyerSystem_CharpApi.Infrastructure.Configurations;

/// <summary>
/// Provides cryptographic helper methods for generating salts, hashing passwords, and verifying passwords.
/// </summary>
public static class CryptoHelper
{
    /// <summary>
    /// Generates a cryptographically secure random salt.
    /// </summary>
    /// <returns>A base64-encoded string representing the generated salt.</returns>
    public static string GenerateSalt()
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return Convert.ToBase64String(salt);
    }

    /// <summary>
    /// Hashes a password using the provided salt and PBKDF2 with HMACSHA256.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The base64-encoded salt to use for hashing.</param>
    /// <returns>A base64-encoded string representing the hashed password.</returns>
    public static string HashPassword(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies a password by comparing its hash with the stored hash and salt.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="storedHash">The base64-encoded hash of the stored password.</param>
    /// <param name="storedSalt">The base64-encoded salt used to hash the stored password.</param>
    /// <returns>True if the password matches the stored hash; otherwise, false.</returns>
    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var hashedPassword = HashPassword(password, storedSalt);
        return hashedPassword == storedHash;
    }
}
