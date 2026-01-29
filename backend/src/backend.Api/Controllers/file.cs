
//git stash pop -> later

//upload file 
using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Domain;
using backend.Application;
namespace backend.Controllers; 

//if file size = small -> buffer in memory
// large = save to disk

//HTTP multipart req(read chunk by chunk)
//Server writes chunks to a temp file (disk)
//File complete on disk -> return status 200
//open file from disk and process (parse/build diagram)

public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
    //TODO: change to array
    public string SelectedOutputTypes { get; set; } = default!;
}

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    // Keep these simple for now (you can move to appsettings later)
    private const long FileSizeLimit = 500L * 1024 * 1024; // 500MB
    private const int BoundaryLengthLimit = 70;

    private readonly ILogger<FileController> _logger;

    private readonly IUploadStore _store;


    public FileController(ILogger<FileController> logger, IUploadStore store) {
        _logger = logger;
        _store = store;
    }

    [HttpPost("generate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Generate([FromForm] UploadRequest req)
    {
        try{
            IFormFile file = req.File;
            //TODO: change to array
            string outputTypes = req.SelectedOutputTypes; 

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            string originalFileName = req.File.FileName;

            string ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext);
            if (extType == 0)
                return BadRequest($"File type {ext} not permitted.");

            //TODO: check if dir exists else create
            string outputDir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
            Directory.CreateDirectory(outputDir);

            string fullFilePath = Path.Combine(outputDir, file.FileName);

            await using (var stream = System.IO.File.Create(fullFilePath)){
                await file.CopyToAsync(stream);
            }
            // await file.CopyToAsync(stream);

            //create obj
            UploadedFile uploadedFile = new()
            {
                OriginalName = originalFileName,
                StoredPath = fullFilePath,
            };

            _store.Files.Add(uploadedFile); 

            //file processing
            try{
                var response = await FileProcessing.ProcessFile(uploadedFile.StoredPath, outputTypes);
                return Ok(new
                    {
                        // fileId = uploadedFile.Id.ToString(),
                        success = true,
                        message = response
                    });
            }catch(Exception e)
            {
                _logger.LogError(e, "FileProcessing failed");
                return StatusCode(500, new { success = false, message = e.Message });
            }
            
        }
        catch (Exception e)
        {
            return BadRequest(e); 
        }
    }


    // [HttpPost("ai")]
    // public Task<IActionResult> TestAi(string prompt)
    // {
    //     FileProcessing FileProcessings = new FileProcessing();
    //     return Task.FromResult<IActionResult>(Ok(new { message = FileProcessings.ProcessFile(prompt) }));
    // }
} 
    
