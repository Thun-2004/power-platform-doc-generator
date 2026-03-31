// Summary: Provides read-only job status information, aggregating per-output progress and errors from the job store.

using backend.Application.DTO;
using backend.Application.Interfaces;

namespace backend.Application.Services;

// Summary: Application service that exposes current status for processing jobs.
public class JobStatusService : IJobStatusService
{
    private readonly IJobStore _jobs;

    // Summary: Creates a JobStatusService that uses the given job store for status lookups.
    public JobStatusService(IJobStore jobs)
    {
        _jobs = jobs;
    }

    // Summary: Retrieves overall job status plus per-output progress and errors for the specified job ID.
    public JobStatusResult GetJobStatus(string jobId)
    {
        var job = _jobs.Get(jobId); // if this throws when not found, handle that here
        if (job == null)
            throw new KeyNotFoundException($"Job '{jobId}' not found.");

        var progress = job.OutputType_FileMeta_Matches.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.Status.ToString() ?? "Unknown"
        );

        var errors = job.OutputType_FileMeta_Matches
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value?.ErrorMessage))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.ErrorMessage ?? string.Empty
            );

        return new JobStatusResult(
            JobId: jobId,
            JobStatus: _jobs.GetJobProgress(jobId).ToString(),
            Progress: progress,
            Errors: errors
        );
    }
}