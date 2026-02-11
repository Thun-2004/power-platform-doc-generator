
//git stash pop -> later

//upload file
using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Domain;
using backend.Application;
using backend.Infrastructure;

using System.Reflection.Metadata;
namespace backend.Controllers; 

enum OutputType
{
    Overview,
    Workflows,
    FAQ,
    Diagrams
}

public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
    public List<string> SelectedOutputTypes { get; set; } = new();
}

//TODO: move to DTO folder
public class ResponseModel<T>
{
    public required int Status { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}




[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    // Keep these simple for now (you can move to appsettings later)
    private const long FileSizeLimit = 500L * 1024 * 1024; // 500MB
    private const int BoundaryLengthLimit = 70;

    private readonly ILogger<FileController> _logger;

    // private readonly IUploadStore _store;

    private readonly IJobStore _jobs;

    private readonly FileProcessing _fileProcessing;

    private readonly record struct FileDescriptor(string Extension, string MimeType, string DownloadName);

    //TODO: fix this 
    private static FileDescriptor CreateFileDescriptor(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".docx" => new FileDescriptor(ext, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Generated_Document.docx"),
            ".xlsx" => new FileDescriptor(ext, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Generated_Document.xlsx"),
            ".pdf" => new FileDescriptor(ext, "application/pdf", "Generated_Document.pdf"),
            ".zip" => new FileDescriptor(ext, "application/zip", "Generated_Document.zip"),
            _ => new FileDescriptor(ext, "application/octet-stream", string.IsNullOrEmpty(ext) ? "Generated_Document.bin" : $"Generated_Document{ext}")
        };
    }

    //constructor injection
    public FileController(ILogger<FileController> logger, IJobStore jobs) {
        _logger = logger;
        _jobs = jobs;
        _fileProcessing = new FileProcessing(jobs);
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

    [HttpPost("generate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Generate([FromForm] UploadRequest req){
        try
        {
            IFormFile file = req.File;
            Console.WriteLine("Selected output types: {0}", string.Join(",", req.SelectedOutputTypes.GetType()));
            List<string> outputTypes = req.SelectedOutputTypes.Select(t => t.Trim()).ToList(); 

            for (int i = 0; i < outputTypes.Count; i++)
            {
                Console.WriteLine(outputTypes[i].ToLowerInvariant());
            }

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

            string parsedDir = Path.Combine(Directory.GetCurrentDirectory(), "parsed_outputs");
            if (!Directory.Exists(parsedDir))
            {
                Directory.CreateDirectory(parsedDir);
            }

            string fullFilePath = Path.Combine(rawinputDir, file.FileName);

            await using (var stream = System.IO.File.Create(fullFilePath)){
                await file.CopyToAsync(stream);
            }

            //init job
            var job = _jobs.Create(outputTypes, fullFilePath);
            
            //file processing
            try{
                //background task
                await Task.Run(() =>
                {
                    _fileProcessing.ProcessFile(outputTypes, job.JobId); 
                });

            }catch(Exception e)
            {
                _logger.LogError(e, "FileProcessing failed");
                return BadRequest($"FileProcessing failed: {e.Message}");
            }

            Dictionary<string, string> mapOutputFilesMetas = new Dictionary<string, string>();
            foreach (var outputType in outputTypes)
            {
                mapOutputFilesMetas[outputType] = $"/api/File/job/{job.JobId}/files/{outputType}";  
            }

            //return output types, file download url
            ResponseModel<JobResponse> response = new ResponseModel<JobResponse>
            {
                Status = 200,
                Message = "File processing started",
                Data = new JobResponse
                {
                    JobId = job.JobId,
                    JobStatus = JobState.Processing.ToString(),
                    JobStatusUrl = $"/api/File/jobstatus/{job.JobId}",
                    OutputFilesMetas = mapOutputFilesMetas
                }
            };

            return Ok(response); 

        }catch(Exception e)
        {
            ResponseModel<object> error = new ResponseModel<object>
            {
                Status = 500,
                Message = e.ToString(),
            };
            return BadRequest(error);
        }
        
    }

    [HttpGet("jobstatus/{jobId}")]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        Dictionary<string, FileMetadata?> fileMeta = _jobs.Get(jobId).OutputType_FileMeta_Matches;
        Dictionary<string, string> mapOutputFilesProgresses = new Dictionary<string, string>();
        foreach(var outputType in fileMeta.Keys)
        {
            mapOutputFilesProgresses[outputType] = fileMeta[outputType]?.Status.ToString() ?? "Unknown";
        }

        try{
            ResponseModel<StatusResponse> response = new ResponseModel<StatusResponse>
                {
                    Status = 200,
                    Message = "Status fetched successfully",
                    Data = new StatusResponse
                    {
                        JobId = jobId,
                        JobStatus = _jobs.GetJobProgress(jobId).ToString(),
                        Progress = mapOutputFilesProgresses
                    }
                };
            return Ok(response); 
        }catch(Exception e)
        {
            ResponseModel<object> error = new ResponseModel<object>
            {
                Status = 500,
                Message = e.ToString(),
            };
            return BadRequest(error);
        }
    }


    //TODO: not sure about design
    [HttpGet("job/{jobId}/files/{outputType}")]
    public async Task<IActionResult> GetJobOutput(string jobId, string outputType)
    {
        try
        {
            Console.WriteLine($"Fetching output file for job {jobId} and output type {outputType}");

            FileMetadata fileMetadata = _jobs.getOutputFile(jobId, outputType);
            var bytes = await System.IO.File.ReadAllBytesAsync(fileMetadata.FilePath); 
            FileDescriptor fileDescriptor = CreateFileDescriptor(fileMetadata.FilePath);

            return Ok(File(
                    bytes,
                    fileMetadata.MimeType,
                    fileDescriptor.DownloadName
                )); 
            
        }catch(Exception e)
        {
            ResponseModel<object> error = new ResponseModel<object>
            {
                Status = 500,
                Message = e.ToString(),
            };
            return BadRequest(error);
        }
    }

    // [HttpGet("getDocument")]
    // public async Task<IActionResult> GetGeneratedFile()
    // {
    //     var response_path = "/Users/benn/Documents/sh38-main/backend/src/backend.Api/rag_outputs/Replybrary_Overview.docx";

    //     var bytes = await System.IO.File.ReadAllBytesAsync(response_path); 

    //     FileDescriptor fileDescriptor = CreateFileDescriptor(response_path);

    //     return File(
    //         bytes,
    //         fileDescriptor.MimeType,
    //         fileDescriptor.DownloadName
    //     );
    // }

} 
    
