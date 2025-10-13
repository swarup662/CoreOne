using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace CoreOne.UI.Helper
{
    public static class FileHelper
    {
        public static async Task SaveCompressedAsync(IFormFile file, string path)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                using var image = await Image.LoadAsync(file.OpenReadStream());
                var encoder = new JpegEncoder { Quality = 70 }; // Compression level
                await image.SaveAsync(path, encoder);
            }
            else
            {
                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }
    }
}
