using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CoreOne.API.Helper
{
    public static class CompressionHelper
    {
        /// <summary>
        /// Compresses a string into Base64 using GZip.
        /// </summary>
        /// <param name="input">The input string to compress.</param>
        /// <returns>Base64 encoded compressed string.</returns>
        public static string CompressToBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(input);
            using var outputStream = new MemoryStream();
            using (var gzip = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }

        /// <summary>
        /// Decompresses a Base64 GZip-compressed string.
        /// </summary>
        /// <param name="base64Input">Base64 string produced by CompressToBase64.</param>
        /// <returns>Original decompressed string.</returns>
        public static string DecompressFromBase64(string base64Input)
        {
            if (string.IsNullOrEmpty(base64Input))
                return string.Empty;

            var compressedBytes = Convert.FromBase64String(base64Input);
            using var inputStream = new MemoryStream(compressedBytes);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzip.CopyTo(outputStream);

            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
    }
}
