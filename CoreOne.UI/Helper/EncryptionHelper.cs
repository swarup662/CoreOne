using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CoreOne.UI.Helper
{

    public class EncryptionHelper
    {
        private readonly string _aesKey;

        public EncryptionHelper(IOptions<SecuritySettings> settings)
        {
            _aesKey = settings.Value.EncryptionKey;
        }

        public string Encrypt(string plain)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_aesKey);
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var input = Encoding.UTF8.GetBytes(plain);
            var cipher = encryptor.TransformFinalBlock(input, 0, input.Length);

            var combined = aes.IV.Concat(cipher).ToArray();
            return Convert.ToBase64String(combined);
        }

        public string Decrypt(string cipher)
        {
            var full = Convert.FromBase64String(cipher);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_aesKey);

            byte[] iv = full.Take(16).ToArray();
            byte[] data = full.Skip(16).ToArray();
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plaintext = decryptor.TransformFinalBlock(data, 0, data.Length);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
    public class SecuritySettings
    {
        public string EncryptionKey { get; set; }
        public string HmacKey { get; set; }
    } 

}
