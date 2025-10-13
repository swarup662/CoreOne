using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace CoreOne.API.Helpers
{
public static class PasswordHelper
    {
        private static readonly byte[] FixedSalt = Encoding.UTF8.GetBytes("1234567890abcdef"); // 16 bytes
        private const int Iterations = 100_000;
        private const int HashSize = 32;

        // Hash the password using fixed salt (for testing only)
        public static string HashPassword(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, FixedSalt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            byte[] result = new byte[FixedSalt.Length + HashSize];
            Buffer.BlockCopy(FixedSalt, 0, result, 0, FixedSalt.Length);
            Buffer.BlockCopy(hash, 0, result, FixedSalt.Length, HashSize);

            return Convert.ToBase64String(result);
        }

        // Verify the password using the fixed salt
        public static bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            // Extract stored hash part (after the fixed salt)
            byte[] expectedHash = new byte[HashSize];
            Buffer.BlockCopy(hashBytes, FixedSalt.Length, expectedHash, 0, HashSize);

            // Re-hash the input password using the same fixed salt
            using var pbkdf2 = new Rfc2898DeriveBytes(password, FixedSalt, Iterations, HashAlgorithmName.SHA256);
            byte[] actualHash = pbkdf2.GetBytes(HashSize);

            // Securely compare the two hashes
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }

}

