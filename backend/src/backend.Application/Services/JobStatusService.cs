using backend.Application.DTO;
using backend.Application.Interfaces;

namespace backend.Application.Services;

public class JobStatusService : IJobStatusService
{
    private readonly IJobStore _jobs;

    public JobStatusService(IJobStore jobs)
    {
        _jobs = jobs;
    }

    public JobStatusResult GetJobStatus(string jobId)
    {
        var job = _jobs.Get(jobId); // if this throws when not found, handle that here
        if (job == null)
            throw new KeyNotFoundException($"Job '{jobId}' not found.");

        var progress = job.OutputType_FileMeta_Matches.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.Status.ToString() ?? "Unknown"
        );

        return new JobStatusResult(
            JobId: jobId,
            JobStatus: _jobs.GetJobProgress(jobId).ToString(),
            Progress: progress
        );
    }
}