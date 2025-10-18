using CoreOne.UI.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Formats;
using System.Reflection.PortableExecutable;
using System.Runtime;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf;          // PdfWriter, PdfDocument, WriterProperties
using iText.Kernel.Pdf.Canvas;   // optional, for canvas operations
using iText.Kernel.Pdf.Canvas.Parser;
using CoreOne.COMMON.Models; // optional

[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly FileUploadSettings _settings;

    public FileUploadController(IOptions<FileUploadSettings> settings)
    {
        _settings = settings.Value;
    }

    // --- Temporary upload for preview ---
    [HttpPost("tempUpload")]
    public async Task<IActionResult> TempUpload([FromForm] string module, [FromForm] IFormFile file)
    {
        var config = _settings.UploadModules
            .FirstOrDefault(x => x.Module.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (config == null) return BadRequest(new { error = "Invalid module" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!config.AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Extension '{ext}' not allowed" });

        if (file.Length > config.MaxSizeInMB * 1024 * 1024)
            return BadRequest(new { error = $"File size exceeded ({config.MaxSizeInMB} MB max)" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        var tempId = TempFileStorage.AddFile(bytes, file.FileName, contentType, module);

        return Ok(new { message = "Temp upload successful", tempId, contentType });
    }


    // --- View temp file for preview/download ---
    [HttpGet("viewTemp")]
    public IActionResult ViewTemp([FromQuery] string tempId, [FromQuery] bool inline = true)
    {
        if (!TempFileStorage.TryGetFile(tempId, out var content, out var fileName, out var contentType, out var _))
            return NotFound("Temp file not found");

        if (inline && (contentType.StartsWith("image/") || contentType == "application/pdf"))
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        }
        else
        {
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
        }

        return File(content, contentType);
    }

    [HttpPost("save")]
    public IActionResult SaveFile([FromForm] string tempId)
    {
        if (!TempFileStorage.TryGetFile(tempId, out var content, out var fileName, out var contentType, out var module))
            return BadRequest(new { error = "File not found" });

        var config = _settings.UploadModules.FirstOrDefault(x => x.Module.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (config == null) return BadRequest(new { error = "Invalid module" });

        if (!Directory.Exists(config.UploadPath)) Directory.CreateDirectory(config.UploadPath);

        // Generate filename with numbering
        int nextNum = 1;
        var files = Directory.GetFiles(config.UploadPath, $"{module}_*.*");
        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f);
            var parts = name.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[^1], out int n))
                if (n >= nextNum) nextNum = n + 1;
        }

        var ext = Path.GetExtension(fileName).ToLower();
        var newFileName = $"{module}_{nextNum:D4}{ext}";
        var savePath = Path.Combine(config.UploadPath, newFileName);

        // Compression
        int compressionLevel = config.CompressionLevel > 0 ? config.CompressionLevel : 50;
        byte[] finalBytes = content;

        if (ext == ".jpg" || ext == ".jpeg")
        {
            using var ms = new MemoryStream(content);
            using var img = Image.FromStream(ms);
            using var outStream = new MemoryStream();

            var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, compressionLevel);
            img.Save(outStream, encoder, encoderParams);

            finalBytes = outStream.ToArray();
        }
        else if (ext == ".pdf")
        {
            try
            {
                int pdfCompression = Math.Clamp(compressionLevel / 10, 0, 9); // iText7 uses 0–9 scale

                using var readerStream = new MemoryStream(content);
                using var reader = new PdfReader(readerStream);

                using var ms = new MemoryStream();
                var writerProps = new WriterProperties()
                    .SetFullCompressionMode(true) // enables better compression
                    .SetCompressionLevel(pdfCompression);

                using var writer = new PdfWriter(ms, writerProps);
                using var pdfDoc = new PdfDocument(reader, writer);

                pdfDoc.Close(); // important to flush all compressed bytes
                finalBytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PDF compression failed: " + ex.Message);
                finalBytes = content; // fallback: save original if compression fails
            }
        }

        // Save final bytes
        System.IO.File.WriteAllBytes(savePath, finalBytes);
        TempFileStorage.RemoveFile(tempId);

        // Determine relative path
        string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        string relativePath = Path.GetFullPath(savePath).StartsWith(wwwrootPath)
            ? savePath.Substring(savePath.IndexOf("wwwroot") + "wwwroot".Length).Replace("\\", "/")
            : savePath;

        return Ok(new { message = "File saved successfully", fileName = newFileName, relativePath });
    }




    // --- Preview from saved path ---
    // --- View saved file for edit/view modal ---
    [HttpGet("viewSaved")]
    public IActionResult ViewSaved([FromQuery] string filePath, [FromQuery] bool inline = true)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest("File path required");

        // Determine full path
        string fullPath = GetFullPath(filePath);
            

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not found");

        var ext = Path.GetExtension(fullPath).ToLower();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        if (inline && (contentType.StartsWith("image/") || contentType == "application/pdf"))
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{Path.GetFileName(fullPath)}\"";
        }
        else
        {
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{Path.GetFileName(fullPath)}\"";
        }

        var fileBytes = System.IO.File.ReadAllBytes(fullPath);
        return File(fileBytes, contentType);
    }
    private string GetFullPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        filePath = filePath.TrimStart('/', '\\');

        if (filePath.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
        {
            string inside = filePath.Substring("wwwroot".Length).TrimStart('/', '\\');
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", inside));
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
    }


}
