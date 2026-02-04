
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
    public string[] SelectedOutputTypes { get; set; } = []; 
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


    //TODO: add type filtering (allowed file types + file size) + error handling 
    //bro u forgot to save to db then return file id to access when what to reuse it
    //use this right now
    [HttpPost("generate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Generate([FromForm] UploadRequest req)
    {
        try{
            IFormFile file = req.File;
            string[] outputTypes = req.SelectedOutputTypes; 

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            string originalFileName = req.File.FileName;

            string ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext);
            if (extType == 0)
                return BadRequest($"File type {ext} not permitted.");

            //TODO: check if dir exists else create
            // string outputDir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
            // Directory.CreateDirectory(outputDir);

            // string fullFilePath = Path.Combine(outputDir, file.FileName);

            // await using FileStream stream = System.IO.File.Create(fullFilePath);
            // await file.CopyToAsync(stream);

            // //create obj
            // UploadedFile uploadedFile = new()
            // {
            //     OriginalName = originalFileName,
            //     StoredPath = fullFilePath,
            // };

            // _store.Files.Add(uploadedFile); 
            

            //TODO: add process file function
            // System.Threading.Tasks.Task<string> response = FileProcessing.ProcessFile(uploadedFile.StoredPath);

            // return Ok(new { fileId = uploadedFile.Id.ToString(), message = "success" });
            // string response = await FileProcessing.ProcessFile(uploadedFile.StoredPath);

            return Ok(new
            {
                // fileId = uploadedFile.Id.ToString(),
                message = "success",
                // response = response
            });
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
    
