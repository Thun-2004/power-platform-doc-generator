
using System.Net;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using backend.Domain;
using backend.Application;
using backend.Infrastructure;
using System.Reflection.Metadata;
using backend.Api.DTO;
using backend.Application.Interfaces;

namespace backend.Api.Controllers; 

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly IJobStatusService _jobStatusService;
    private readonly IJobOutputService _jobOutputService; 

    public FileController(IUploadService uploadService, IJobStatusService jobStatusService, IJobOutputService jobOutputService) {
        _uploadService = uploadService; 
        _jobStatusService = jobStatusService;
        _jobOutputService = jobOutputService; 
    }

    [HttpPost("generate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Generate([FromForm] UploadRequest req,  CancellationToken ct){
        if (req.File == null || req.File.Length == 0)
            return BadRequest("File is required");

        List<string> outputTypes = req.SelectedOutputTypes.Select(t => t.Trim()).ToList();
        try
        {
            var job = await _uploadService.StartJobAsync(req.File, outputTypes, ct); 
            return Ok(new ResponseModel<JobResponse>
            {
                Status = 200,
                Message = "File processing started",
                Data = new JobResponse
                {
                    JobId = job.JobId,
                    JobStatus = JobState.Processing.ToString(),
                    JobStatusUrl = $"/api/File/jobstatus/{job.JobId}",
                    OutputFilesMetas = job.OutputFilesMetas
                }
            });
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("jobstatus/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        try
        {
            var result = _jobStatusService.GetJobStatus(jobId);

            return Ok(new ResponseModel<StatusResponse>
            {
                Status = 200,
                Message = "Status fetched successfully",
                Data = new StatusResponse
                {
                    JobId = result.JobId,
                    JobStatus = result.JobStatus,
                    Progress = result.Progress.ToDictionary(k => k.Key, v => v.Value)
                }
            });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new ResponseModel<object> { Status = 404, Message = e.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ResponseModel<object> { Status = 500, Message = e.Message });
        }
    }

    [HttpGet("job/{jobId}/files/{outputType}")]
    public async Task<IActionResult> GetJobOutput(string jobId, string outputType, CancellationToken ct)
    {
        try
        {
            var jobOutput = await _jobOutputService.GetJobOutputAsync(jobId, outputType, ct); 

            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

            return File(
                    jobOutput.Content,
                    jobOutput.MimeType,
                    jobOutput.DownloadName
                ); 
            
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


    // [HttpGet("job/{jobId}/files/{outputType}")]
    // public async Task<IActionResult> GetJobOutput(string jobId, string outputType)
    // {
    //     try
    //     {
    //         Console.WriteLine($"Fetching output file for job {jobId} and output type {outputType}");

    //         FileMetadata fileMetadata = _jobs.getOutputFile(jobId, outputType);
    //         var bytes = await System.IO.File.ReadAllBytesAsync(fileMetadata.FilePath); 
    //         FileDescriptor fileDescriptor = CreateFileDescriptor(fileMetadata.FilePath);

    //         return Ok(File(
    //                 bytes,
    //                 fileMetadata.MimeType,
    //                 fileDescriptor.DownloadName
    //             )); 
            
    //     }catch(Exception e)
    //     {
    //         ResponseModel<object> error = new ResponseModel<object>
    //         {
    //             Status = 500,
    //             Message = e.ToString(),
    //         };
    //         return BadRequest(error);
    //     }
    // }

    // test function
    // [HttpGet("getDocument")]
    // public async Task<IActionResult> GetGeneratedFile()
    // {
    //     //change this to your excel file path
    //     var response_path = "/Users/benn/Documents/sh38-main/backend/src/backend.Infrastructure/FileStorages/RAGOutputs/Replybrary_Overview.docx";

    //     var bytes = await System.IO.File.ReadAllBytesAsync(response_path); 

    //     FileDescriptor fileDescriptor = CreateFileDescriptor(response_path);

    //     return File(
    //         bytes,
    //         fileDescriptor.MimeType,
    //         fileDescriptor.DownloadName
    //     );
    // }
}
    
