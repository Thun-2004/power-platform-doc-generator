// Summary: Coordinates upload of solution files, job creation, and background processing to generate requested outputs.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using backend.Application.DTO;
using backend.Application.Interfaces;
using backend.Domain;
using backend.Application.LLM;
using backend.Application.Helpers;
using backend.Application.Config;

namespace backend.Application.Services;

// Summary: Application service that manages file uploads and regeneration requests for output documents.
public class UploadService : IUploadService
{
    private readonly ILogger<UploadService> _logger;
    private readonly IJobStore _jobs;
    private readonly FileProcessing _fileProcessing;
    private readonly IFileStorage _storage;
    private readonly SharedOptions _sharedConfig;

    // Summary: Creates an UploadService with dependencies for logging, job tracking, file processing, storage, and shared configuration.
    public UploadService(
        ILogger<UploadService> logger,
        IJobStore jobs,
        FileProcessing fileProcessing,
        IFileStorage storage,
        IOptions<SharedOptions> sharedConfig)
    {
        _logger = logger;
        _jobs = jobs;
        _fileProcessing = fileProcessing;
        _storage = storage;
        _sharedConfig = sharedConfig.Value;
    }

    // Summary: Validates and saves an uploaded file, creates a job, starts background processing, and returns initial output file URLs.
    public async Task<JobStartResult> StartJobAsync(IFormFile file, List<string> outputTypes, string LlmModel, IReadOnlyDictionary<string, string>? outputPrompts, CancellationToken ct)
    {
        // normalize
        outputTypes = outputTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        // validate basic
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required");

        // validate extension (same logic as controller)
        var originalFileName = file.FileName;
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

        // validate against allowed types from config
        var allowed = _sharedConfig?.AllowedUploadedFileTypes ?? Array.Empty<string>();
        if (allowed.Length > 0 && !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"File type {ext} not permitted.");

        // save upload (handles directories + unique naming)
        var fullFilePath = await _storage.SaveUploadAsync(file, ct);

        // create job
        var job = _jobs.Create(outputTypes, Path.GetFileNameWithoutExtension(originalFileName), fullFilePath);

        //validate test before background job
        FileValidation.ValidateSolutionZipOrThrow(job.ZipFilePath);
        // background job
        _ = Task.Run(async () =>
        {
            try
            {
                await _fileProcessing.ProcessFile(outputTypes, job.JobId, LlmModel, outputPrompts);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "FileProcessing failed for job {JobId}", job.JobId);
            }
        }, CancellationToken.None);

        // build output meta urls
        var outputFilesMetas = outputTypes.ToDictionary(
            t => t,
            t => $"/api/File/job/{job.JobId}/files/{t}"
        );

        return new JobStartResult(job.JobId, outputFilesMetas);
    }

    // Summary: Validates an existing job and requested output types, restarts background processing, and returns updated output file URLs.
    public Task<JobStartResult> RegenerateJobAsync(string jobId, List<string> outputTypes, string llmModel, IReadOnlyDictionary<string, string>? outputPrompts, CancellationToken ct)
    {
        var job = _jobs.Get(jobId);
        if (job == null)
            throw new ArgumentException("Job not found");

        outputTypes = outputTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (outputTypes.Count == 0)
            throw new ArgumentException("At least one output type is required.");

        // validate test before background job
        FileValidation.ValidateSolutionZipOrThrow(job.ZipFilePath);

        // Mark outputs as Processing before returning so the first poll is not still "Failed"
        // from the previous run (ProcessFile runs in Task.Run and would race the UI poll).
        foreach (var t in outputTypes)
            _jobs.UpdateSingleOutputFileProgress(job.JobId, t, JobState.Processing);

        // background job
        _ = Task.Run(async () =>
        {
            try
            {
                await _fileProcessing.ProcessFile(outputTypes, job.JobId, llmModel, outputPrompts);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "FileProcessing failed for job {JobId}", job.JobId);
            }
        }, CancellationToken.None);

        var outputFilesMetas = outputTypes.ToDictionary(
            t => t,
            t => $"/api/File/job/{job.JobId}/files/{t}"
        );

        return Task.FromResult(new JobStartResult(job.JobId, outputFilesMetas));
    }
}