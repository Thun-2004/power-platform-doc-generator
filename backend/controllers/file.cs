
//upload file 
using System.Net;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Utilities; 
namespace backend.controllers; 

//if file size = small -> buffer in memory 
// large = save to disk 

//HTTP multipart req(read chunk by chunk)
//Server writes chunks to a temp file (disk)
//File complete on disk -> return status 200
//open file from disk and process (parse/build diagram)

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    // Keep these simple for now (you can move to appsettings later)
    private static readonly string[] PermittedExtensions = [".zip", ".msapp"];
    private const long FileSizeLimit = 500L * 1024 * 1024; // 500MB
    private const int BoundaryLengthLimit = 70;

    [HttpPost("uploadFile")]
    //[DisableFormValueModelBinding] // from the docs sample; OK to keep if you have it
    public async Task<IActionResult> UploadPhysical()
    {
        if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            return BadRequest("Expected a multipart/form-data request.");

        // Where to save locally
        var targetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "uploads-test"
        );
        Directory.CreateDirectory(targetDir);

        // Setup multipart reader
        var boundary = MultipartRequestHelper.GetBoundary(
            MediaTypeHeaderValue.Parse(Request.ContentType),
            BoundaryLengthLimit
        );
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);

        string? outputType = null; // example extra field
        string? savedPath = null;
        string? originalFileName = null;
        long totalBytesWritten = 0;

        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                continue;

            // Handle normal form fields (e.g. outputType)
            if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
            {
                var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
                using var sr = new StreamReader(section.Body);
                var value = await sr.ReadToEndAsync();

                if (string.Equals(key, "outputType", StringComparison.OrdinalIgnoreCase))
                    outputType = value;

                continue;
            }

            // Handle file field
            if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
            {
                originalFileName = contentDisposition.FileName.Value ?? "upload";
                var safeDisplayName = WebUtility.HtmlEncode(originalFileName);

                var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (!PermittedExtensions.Contains(ext))
                    return BadRequest($"File type '{safeDisplayName}' not permitted.");

                // Choose a safe server-side filename
                var serverFileName = $"{Guid.NewGuid():N}{ext}";
                savedPath = Path.Combine(targetDir, serverFileName);

                // Stream directly to disk (no buffering the whole file)
                await using var targetStream = System.IO.File.Create(savedPath);
                totalBytesWritten = await CopyToWithLimitAsync(section.Body, targetStream, FileSizeLimit);

                return Ok(new
                {
                    message = "Uploaded",
                    fileName = safeDisplayName,
                    savedAs = serverFileName,
                    bytes = totalBytesWritten,
                    outputType
                });
            }
        }

        return BadRequest("No file section found in the request.");
    }

    private static async Task<long> CopyToWithLimitAsync(Stream source, Stream destination, long limit)
    {
        var buffer = new byte[81920];
        long total = 0;

        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > limit)
                throw new InvalidDataException($"File too large. Limit is {limit} bytes.");

            await destination.WriteAsync(buffer.AsMemory(0, read));
        }

        return total;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload2([FromForm] UploadRequest req)
    {
        var file = req.File; 
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var dir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
        Directory.CreateDirectory(dir);

        // Don’t trust client filename in prod; for now OK for local test
        var fullFilePath = Path.Combine(dir, file.FileName);

        await using var stream = System.IO.File.Create(fullFilePath);
        await file.CopyToAsync(stream);

        return Ok(new { message = "success" });
    }
    
} 

public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
}
    
    //end point for sending converted file type 
    // public async Task<IActionResult> ()
