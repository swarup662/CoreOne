using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CoreOne.API.Helpers
{
    public class PasswordHelper
    {
        private readonly byte[] _masterKeyBytes;
        private readonly int _keySizeBits;
        private readonly int _ivLengthBytes;

        // IConfiguration injected (e.g. in DI)
        public PasswordHelper(IConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            string masterKey = config["ReversiblePassword:MasterKey"]
                ?? throw new ArgumentException("ReversiblePassword:MasterKey must be provided in configuration.");

            if (!int.TryParse(config["ReversiblePassword:KeySize"], out _keySizeBits))
                _keySizeBits = 256; // default

            if (!int.TryParse(config["ReversiblePassword:IVLength"], out _ivLengthBytes))
                _ivLengthBytes = 16; // default 16 bytes for AES

            // Normalize master key bytes
            _masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);
        }

        // Derive per-user AES key from master key + saltKey using SHA-256
        private byte[] DeriveKey(string saltKey)
        {
            if (saltKey == null) saltKey = string.Empty;

            using var sha = SHA256.Create();
            // Combine master key and saltKey bytes
            byte[] combined = new byte[_masterKeyBytes.Length + Encoding.UTF8.GetByteCount(saltKey)];
            Buffer.BlockCopy(_masterKeyBytes, 0, combined, 0, _masterKeyBytes.Length);
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(saltKey), 0, combined, _masterKeyBytes.Length,
                Encoding.UTF8.GetByteCount(saltKey));

            byte[] hash = sha.ComputeHash(combined);

            // Ensure key length matches required AES key length (128/192/256 bits)
            int keyBytesNeeded = _keySizeBits / 8;
            if (keyBytesNeeded <= 0 || keyBytesNeeded > hash.Length)
                throw new InvalidOperationException("Unsupported KeySize in configuration.");

            // If SHA-256 produces 32 bytes and keyBytesNeeded <=32, just use first keyBytesNeeded bytes.
            byte[] key = new byte[keyBytesNeeded];
            Buffer.BlockCopy(hash, 0, key, 0, keyBytesNeeded);
            return key;
        }

        // Encrypt: returns Base64( IV || Ciphertext )
        public string EncryptPassword(string plainText, string saltKey)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));

            byte[] key = DeriveKey(saltKey);

            using Aes aes = Aes.Create();
            aes.KeySize = _keySizeBits;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Generate random IV for each encryption
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext so we can decrypt later
            byte[] result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        // Decrypt: accepts Base64( IV || Ciphertext )
        public string DecryptPassword(string cipherTextBase64, string saltKey)
        {
            if (cipherTextBase64 == null) throw new ArgumentNullException(nameof(cipherTextBase64));

            byte[] allBytes;
            try
            {
                allBytes = Convert.FromBase64String(cipherTextBase64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("cipherTextBase64 is not a valid Base64 string.");
            }

            if (allBytes.Length <= _ivLengthBytes)
                throw new ArgumentException("Invalid cipher text (too short).");

            byte[] iv = new byte[_ivLengthBytes];
            Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);

            int cipherLen = allBytes.Length - iv.Length;
            byte[] cipherBytes = new byte[cipherLen];
            Buffer.BlockCopy(allBytes, iv.Length, cipherBytes, 0, cipherLen);

            byte[] key = DeriveKey(saltKey);

            using Aes aes = Aes.Create();
            aes.KeySize = _keySizeBits;
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // Verify: returns true if provided plain password matches stored ciphertext.
        // This method DOES NOT return the plaintext to caller. It only does an internal decrypt+secure compare.
        public bool VerifyPassword(string plainText, string storedCipherBase64, string saltKey)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (storedCipherBase64 == null) throw new ArgumentNullException(nameof(storedCipherBase64));

            try
            {
                // Decrypt stored value (internal use only)
                string decryptedStored = DecryptPassword(storedCipherBase64, saltKey);

                // Convert both to bytes and use constant-time comparison
                byte[] providedBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] storedBytes = Encoding.UTF8.GetBytes(decryptedStored);

                // If lengths differ, comparison should still be done in constant time.
                if (providedBytes.Length != storedBytes.Length)
                {
                    // Create a buffer to compare same length as storedBytes to avoid leaking length-based timing.
                    // But FixedTimeEquals requires equal-length arrays. We'll do a fixed-time check manually:
                    // Build a padded array and still call FixedTimeEquals with equal lengths.
                    int maxLen = Math.Max(providedBytes.Length, storedBytes.Length);
                    byte[] a = new byte[maxLen];
                    byte[] b = new byte[maxLen];
                    Buffer.BlockCopy(providedBytes, 0, a, 0, providedBytes.Length);
                    Buffer.BlockCopy(storedBytes, 0, b, 0, storedBytes.Length);
                    return CryptographicOperations.FixedTimeEquals(a, b) && providedBytes.Length == storedBytes.Length;
                }

                return CryptographicOperations.FixedTimeEquals(providedBytes, storedBytes);
            }
            catch
            {
                // Any failure (bad format, decryption error) => verification false
                return false;
            }
        }
    }
}
