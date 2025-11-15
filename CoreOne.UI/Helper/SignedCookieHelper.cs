using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CoreOne.UI.Helper
{
   
    public class SignedCookieHelper
    {
        private readonly EncryptionHelper _enc;
        private readonly string _hmacKey;

        public SignedCookieHelper(
            EncryptionHelper enc,
            IOptions<SecuritySettings> settings)
        {
            _enc = enc;
            _hmacKey = settings.Value.HmacKey;
        }

        public string CreateSignedValue(string value)
        {
            string encrypted = _enc.Encrypt(value);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(encrypted)));

            return encrypted + "." + signature;
        }

        public string? ValidateAndGet(string signedValue)
        {
            if (!signedValue.Contains(".")) return null;

            var parts = signedValue.Split('.');
            string encrypted = parts[0];
            string signature = parts[1];

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            string expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(encrypted)));

            if (expected != signature)
                return null; // TAMPERED

            return _enc.Decrypt(encrypted);
        }
    }

}
