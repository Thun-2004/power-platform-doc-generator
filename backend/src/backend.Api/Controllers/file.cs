
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
        if (req?.File == null || req.File.Length == 0)
            return BadRequest("File is required");

        // Parse "overview" or "overview: add conclusion at the end" into types + per-type prompts
        var outputTypes = new List<string>();
        var outputPrompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var selectedItems = req.SelectedOutputTypes ?? new List<string>();
        foreach (var item in selectedItems.Select(t => (t ?? "").Trim()).Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var colonIdx = item.IndexOf(':');
            string type;
            string? promptPart = null;
            if (colonIdx > 0)
            {
                type = item.Substring(0, colonIdx).Trim().ToLowerInvariant();
                promptPart = item.Substring(colonIdx + 1).Trim();
            }
            else
                type = item.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(type)) continue;
            if (!outputTypes.Contains(type))
                outputTypes.Add(type);
            if (!string.IsNullOrWhiteSpace(promptPart))
                outputPrompts[type] = promptPart;
        }

        try
        {
            var job = await _uploadService.StartJobAsync(req.File, outputTypes, req.LlmModel, outputPrompts, ct); 
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
        }catch (DirectoryNotFoundException e){
            return Problem(
                detail: e.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request"
            );
        }
        catch (ArgumentException e)
        {
            return Problem(
                detail: e.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request"
            );
        }
        catch (Exception e)
        {
            var message = e.InnerException?.Message ?? e.Message;
            return Problem(
                detail: message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request"
            );
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
                    Progress = result.Progress.ToDictionary(k => k.Key, v => v.Value),
                    Errors = result.Errors.ToDictionary(k => k.Key, v => v.Value)
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
        }
        catch (JobOutputFailedException e)
        {
            var error = new ResponseModel<object>
            {
                Status = 500,
                Message = e.Message,
            };
            return StatusCode(500, error);
        }
        catch (JobOutputNotReadyException e)
        {
            var error = new ResponseModel<object>
            {
                Status = 409,
                Message = e.Message,
            };
            return StatusCode(409, error);
        }
        catch(Exception e)
        {
            ResponseModel<object> error = new ResponseModel<object>
            {
                Status = 500,
                Message = e.ToString(),
            };
            return BadRequest(error);
        }
    }

    [HttpPost("regenerate")]
    public async Task<IActionResult> Regenerate([FromBody] RegenerateRequest req, CancellationToken ct)
    {
            // string jobId, string outputType, string llmModel, string outputPrompts, CancellationToken ct
            //split outputType and outputPrompts
            var outputTypes = new List<string>();
            var outputPrompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var selectedItems = req.SelectedOutputTypes ?? new List<string>();
            foreach (var item in selectedItems.Select(t => (t ?? "").Trim()).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var colonIdx = item.IndexOf(':');
                string type;
                string? promptPart = null;
                if (colonIdx > 0)
                {
                    type = item.Substring(0, colonIdx).Trim().ToLowerInvariant();
                    promptPart = item.Substring(colonIdx + 1).Trim();
                }
                else
                    type = item.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type)) continue;
                if (!outputTypes.Contains(type))
                    outputTypes.Add(type);
                if (!string.IsNullOrWhiteSpace(promptPart))
                    outputPrompts[type] = promptPart;
            }

            try
            {
                var job = await _uploadService.RegenerateJobAsync(req.jobId, outputTypes, req.LlmModel, outputPrompts, ct);

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
            catch (Exception e)
            {
                var message = e.InnerException?.Message ?? e.Message;
                return Problem(
                    detail: message,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request"
                );
            }
    }
}
    
