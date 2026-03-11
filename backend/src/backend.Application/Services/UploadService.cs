using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using backend.Application.DTO;
using backend.Application.Interfaces;
using backend.Domain;
using backend.Application.LLM; 
using backend.Application.Helpers; 

namespace backend.Application.Services;

public class UploadService : IUploadService
{
    private readonly ILogger<UploadService> _logger;
    private readonly IJobStore _jobs;
    private readonly FileProcessing _fileProcessing;
    private readonly IFileStorage _storage;

    public UploadService(
        ILogger<UploadService> logger,
        IJobStore jobs,
        FileProcessing fileProcessing,
        IFileStorage storage)
    {
        _logger = logger;
        _jobs = jobs;
        _fileProcessing = fileProcessing;
        _storage = storage;
    }

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

        PermittedExtensions extType = PermittedFiletypeConversion.ToExtension(ext);
        if (extType == 0)
            throw new ArgumentException($"File type {ext} not permitted.");

        // save upload (handles directories + unique naming)
        var fullFilePath = await _storage.SaveUploadAsync(file, ct);

        // create job
        var job = _jobs.Create(outputTypes, Path.GetFileNameWithoutExtension(originalFileName), fullFilePath);

        //validate test before background job
        FileOperation.ValidateSolutionZipOrThrow(job.ZipFilePath);
        // background job
        _ = Task.Run(async () =>
        {
            try
            {
                await _fileProcessing.ProcessFile(outputTypes, job.JobId, LlmModel, outputPrompts);
            }
            //TODO: more descriptive error handling
            catch (Exception e)
            {
                _logger.LogError(e, "FileProcessing failed for job {JobId}", job.JobId);
                // _jobs.FailJob(job.JobId, e.Message);
            }
        }, CancellationToken.None);

        // build output meta urls
        var outputFilesMetas = outputTypes.ToDictionary(
            t => t,
            t => $"/api/File/job/{job.JobId}/files/{t}"
        );

        return new JobStartResult(job.JobId, outputFilesMetas);
    }
}