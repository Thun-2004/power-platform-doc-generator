
//git stash pop -> later

//upload file 
using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Domain;
using backend.Application;
using System.Reflection.Metadata;
namespace backend.Controllers; 


enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

enum DocGenStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

enum OutputType
{
    Overview,
    Workflows,
    FAQ,
    Diagrams
}

//TODO: move to DTO folder
public class ResponseModel<T>
{
    public required int Status { get; set; }
    public required string Message { get; set; }

    public T? Data { get; set; }

    public List<string>? Errors { get; set; }
}

public class JobResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required string JobStatusUrl { get; set; }
    public List<FileMetadata>? Files { get; set; }
}

public class FileMetadata
{
    public required string Type { get; set; }
    public required string DownloadUrl { get; set; }
}

public class StatusResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required Dictionary<string, string> Progress { get; set; }
}


public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
    public List<string> SelectedOutputTypes { get; set; } = new();
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

    private readonly record struct FileDescriptor(string Extension, string MimeType, string DownloadName);


    //TODO: fix this 
    private static FileDescriptor CreateFileDescriptor(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".docx" => new FileDescriptor(ext, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Generated_Document.docx"),
            ".pdf" => new FileDescriptor(ext, "application/pdf", "Generated_Document.pdf"),
            ".zip" => new FileDescriptor(ext, "application/zip", "Generated_Document.zip"),
            ".msapp" => new FileDescriptor(ext, "application/octet-stream", "Generated_Document.msapp"),
            _ => new FileDescriptor(ext, "application/octet-stream", string.IsNullOrEmpty(ext) ? "Generated_Document.bin" : $"Generated_Document{ext}")
        };
    }

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
            //TODO: set request time out for each end point 
            Console.WriteLine("Selected output types: {0}", string.Join(",", req.SelectedOutputTypes.GetType()));
            List<string> outputTypes = req.SelectedOutputTypes; 

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            string originalFileName = req.File.FileName;

            string ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext);
            if (extType == 0)
                return BadRequest($"File type {ext} not permitted.");


            //check/create dirs
            string rawinputDir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
            if (!Directory.Exists(rawinputDir))
            {
                Directory.CreateDirectory(rawinputDir);
            }

            string ragoutDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");
            if (!Directory.Exists(ragoutDir))
            {
                Directory.CreateDirectory(ragoutDir);
            }

            string parsedDir = Path.Combine(Directory.GetCurrentDirectory(), "parsed_output");
            if (!Directory.Exists(parsedDir))
            {
                Directory.CreateDirectory(parsedDir);
            }

            string fullFilePath = Path.Combine(rawinputDir, file.FileName);

            await using (var stream = System.IO.File.Create(fullFilePath)){
                await file.CopyToAsync(stream);
            }

            //create obj
            UploadedFile uploadedFile = new()
            {
                OriginalName = originalFileName,
                StoredPath = fullFilePath,
            };

            _store.Files.Add(uploadedFile); 

            //file processing
            try{
                var response_path = await FileProcessing.ProcessFile(uploadedFile.StoredPath , outputTypes);
                
                if (!System.IO.File.Exists(response_path))
                    return BadRequest("Generated file not found.");

                var bytes = await System.IO.File.ReadAllBytesAsync(response_path); 

                FileDescriptor fileDescriptor = CreateFileDescriptor(response_path);

                return File(
                    bytes,
                    fileDescriptor.MimeType,
                    fileDescriptor.DownloadName
                );
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

    [HttpGet("getDocument")]
    public async Task<IActionResult> GetJobOutput()
    {
        var response_path = "C:/Users/Kylej/OneDrive/Documents/teamstuff/sh38-main/backend/src/backend.Api/rag_outputs/Replybrary_Overview.docx";
 
        var bytes = await System.IO.File.ReadAllBytesAsync(response_path);
 
        FileDescriptor fileDescriptor = CreateFileDescriptor(response_path);
 
        return File(
            bytes,
            fileDescriptor.MimeType,
            fileDescriptor.DownloadName
        );
    }

    //recreateion 
    // [HttpPost("generate2")]
    // [Consumes("multipart/form-data")]
    // public async IActionResult Generate2([FromForm] UploadRequest req){
    //     try
    //     { 

    //         IFormFile file = req.File;
    //         Console.WriteLine("Selected output types: {0}", string.Join(",", req.SelectedOutputTypes.GetType()));
    //         List<string> outputTypes = req.SelectedOutputTypes; 

    //         if (file == null || file.Length == 0)
    //             throw BadRequest("File is required");

    //         string originalFileName = req.File.FileName;

    //         string ext = Path.GetExtension(originalFileName).ToLowerInvariant();
    //         PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext);
    //         if (extType == 0)
    //             throw BadRequest($"File type {ext} not permitted.");


    //         //check/create dirs
    //         string rawinputDir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
    //         if (!Directory.Exists(rawinputDir))
    //         {
    //             Directory.CreateDirectory(rawinputDir);
    //         }

    //         string ragoutDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");
    //         if (!Directory.Exists(ragoutDir))
    //         {
    //             Directory.CreateDirectory(ragoutDir);
    //         }

    //         string parsedDir = Path.Combine(Directory.GetCurrentDirectory(), "parsed_outputs");
    //         if (!Directory.Exists(parsedDir))
    //         {
    //             Directory.CreateDirectory(parsedDir);
    //         }

    //         string fullFilePath = Path.Combine(rawinputDir, file.FileName);

    //         await using (var stream = System.IO.File.Create(fullFilePath)){
    //             await file.CopyToAsync(stream);
    //         }

    //         //file processing
    //         try{
    //             var response_path = await FileProcessing.ProcessFile(fullFilePath , outputTypes);
                
    //             if (!System.IO.File.Exists(response_path))
    //                 return BadRequest("Generated file not found.");

    //             var bytes = await System.IO.File.ReadAllBytesAsync(response_path); 

    //             FileDescriptor fileDescriptor = CreateFileDescriptor(response_path);

    //             // return File(
    //             //     bytes,
    //             //     fileDescriptor.MimeType,
    //             //     fileDescriptor.DownloadName
    //             // );
    //         }catch(Exception e)
    //         {
    //             _logger.LogError(e, "FileProcessing failed");
    //             throw BadRequest($"FileProcessing failed: {e.Message}");
    //         }

    //         //return output types, file download url
    //         string jobId = Guid.NewGuid().ToString();
    //         ResponseModel<JobResponse> response = new ResponseModel<JobResponse>
    //         {
    //             Status = 200,
    //             Message = "File processing started",
    //             Data = new JobResponse
    //             {
    //                 JobId = jobId,
    //                 JobStatus = JobStatus.Processing.ToString(),
    //                 JobStatusUrl = $"/api/File/jobstatus/{jobId}",

    //                 Files = outputTypes.Select(
    //                     outType => new FileMetadata
    //                     {
    //                         Type = outType,
    //                         DownloadUrl = $"/api/File/job/{jobId}/files/{outType}"
    //                     }
    //                 ).ToList()
    //             }
    //         };

    //         return Ok(response); 

    //     }catch(Exception e)
    //     {
    //         ResponseModel<JobResponse> error = new ResponseModel<JobResponse>
    //         {
    //             Status = 500,
    //             Message = e.ToString(),
    //         };
    //         return BadRequest(error);
    //     }
        
    // }

    // [HttpGet("jobstatus/{jobId}")]
    // public Task<IActionResult> GetJobStatus(string jobId)
    // {

    //     try{
    //         ResponseModel<JobResponse> response = new ResponseModel<JobResponse>
    //             {
    //                 Status = 200,
    //                 Message = "Status fetched successfully",
    //                 Data = new JobResponse
    //                 {
    //                     JobId = jobId,
    //                     JobStatus = JobStatus.Processing.ToString(),

    //                     Progress =
    //                     {
    //                         [OutputType.Overview.ToString()] = DocGenStatus.Completed.ToString(),
    //                         [OutputType.Workflows.ToString()] = DocGenStatus.Processing.ToString(),
    //                         [OutputType.FAQ.ToString()] = DocGenStatus.Pending.ToString()
    //                     }
    //                 }
    //             };
    //         return Ok(response); 

    //     }catch(Exception e)
    //     {
    //         ResponseModel<JobResponse> error = new ResponseModel<JobResponse>
    //         {
    //             Status = 500,
    //             Message = e.ToString(),
    //         };
    //         return BadRequest(error);
            
    //     }
    // }


    // [HttpGet("job/{jobId}/files/{outType}")]
    // public async Task<IActionResult> GetJobOutput(string jobId, string outType)
    // {
    //     try
    //     {
    //         return File; 
            
    //     }catch(Exception e)
    //     {
    //         ResponseModel<JobResponse> error = new ResponseModel<JobResponse>
    //         {
    //             Status = 500,
    //             Message = e.ToString(),
    //         };
    //         return BadRequest(error);
    //     }
    // }

} 
    
