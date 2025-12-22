
//upload file 
using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Utilities;
using backend.Domain;
namespace backend.controllers; 

//if file size = small -> buffer in memory 
// large = save to disk 

//HTTP multipart req(read chunk by chunk)
//Server writes chunks to a temp file (disk)
//File complete on disk -> return status 200
//open file from disk and process (parse/build diagram)




public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    // Keep these simple for now (you can move to appsettings later)
    private const long FileSizeLimit = 500L * 1024 * 1024; // 500MB
    private const int BoundaryLengthLimit = 70;

    //FIX: current files stored in memory -> shift to disk(sqlite or ms azure) where it allows fast read/write
    // private readonly List<UploadedFile> selectedFiles = []; 

    private readonly ILogger<FileController> _logger;

    private readonly IUploadStore _store;


    public FileController(ILogger<FileController> logger, IUploadStore store) {
        _logger = logger;
        _store = store;
    }


    // Upload to disk 
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

            // Handle normal form fields
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
                PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext); 
                if (extType == 0)
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
        try{
            var file = req.File;
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            //add
            string originalFileName = req.File.FileName; 
            var safeDisplayName = WebUtility.HtmlEncode(originalFileName); //FIX: not sure why have it

            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext); 
            if (extType == 0)
                return BadRequest($"File type '{safeDisplayName}' not permitted.");
            //add
            
            var dir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
            Directory.CreateDirectory(dir);

            // Don’t trust client filename in prod; for now OK for local test
            var fullFilePath = Path.Combine(dir, file.FileName);

            await using var stream = System.IO.File.Create(fullFilePath);
            await file.CopyToAsync(stream);

            //create obj

            UploadedFile uploadedFile = new UploadedFile
            { 
                OriginalName = originalFileName,
                StoredPath = fullFilePath,
            }; 

            _store.Files.Add(uploadedFile); 
            
            return Ok(new { fileId = uploadedFile.Id.ToString(), message = "success" }); //FIX: return name + status
        }
        catch (Exception e)
        {
            return BadRequest(e); 
        }
    }

    [HttpPost("modes")]
    public async Task<IActionResult> selectModes([FromForm] string fileId, [FromForm] string[] modes)
    {
        var file = _store.Files.FirstOrDefault(f => f.Id.ToString() == fileId); 
        _logger.LogInformation("Test here");
        foreach(var s in _store.Files)
        {
            _logger.LogInformation("File: {File}", s);
        }
        if (file == null)
            return NotFound(new { code = 404, message = $"File {fileId} not found"}); 
 
        file.Modes = modes.Select(ModetypeConversion.ToModeType).ToList();

        return Ok(new { code = 200, message = "success", output=modes }); 
            

        //FIX: return processed output? 
        //Results = processedFile();
        // try
        // {
        //     // TODO: 
        //     // var result = ProcessFile(file);
        //     return Ok(new { code = 200, message = "success" }); 
            
        // }catch(Exception e)
        // {
        //     //FIX: create enum for error message type
        //     return StatusCode(StatusCodes.Status500InternalServerError, new { code = 500, message = "Processing failed", error = e.Message });
        // }
    }
  
    
} 
    
    //end point for sending converted file type 
    // public async Task<IActionResult> ()
