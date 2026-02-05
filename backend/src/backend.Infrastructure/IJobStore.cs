
using System.Collections.Concurrent;


namespace backend.Infrastructure; 


public interface IJobStore
{
    JobRecord Create(List<string> outputTypes); 
    JobRecord Get(string jobId); 
    void Update(JobRecord job); 

    void UpdateFileProgress(string jobId, string outputType, JobState filProgress); 

    void setOutputFile(string jobId, string outputType, FileMetadata fileMeta);
} 

public class JobStore : IJobStore
{

    //link job id to job for fast look up : O(1)
    private readonly ConcurrentDictionary<string, JobRecord> _jobs = new(); 

    public JobRecord Create(List<string> outputTypes)
    {
        var job = new JobRecord
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobState.Processing,
            Progress = outputTypes.ToDictionary(
                outtype => outtype, 
                _ => JobState.Pending
            )
        }; 

        _jobs[job.JobId] = job;
        return job;
    }

    public JobRecord? Get(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    public void Update(JobRecord job)
    {
        return _jobs[job.JobId] = job;
    }

    public JobState GetJobState(string jobId)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        return job.Status;
    }

    public JobState GetSingleFileProgressByType(string jobId, string outputType)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        if (!job.Progress.TryGetValue(outputType, out var progress))
            throw new Exception("Output type not found");

        return progress;
    }


    public void UpdateFileProgress(string jobId, string outputType, JobState fileProgress)
    {
        //use lock
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            return;

        if (!job.Progress.ContainsKey(outputType))
            return; 

        job.Progress[outputType] = fileProgress; 

        if (job.Progress.Values.Any(ValueTask => ValueTask == JobState.Failed))
        {
            job.Status = JobState.Failed; 
        }else if (job.Progress.Values.All(ValueTask => ValueTask == JobState.Completed))
        {
            job.Status = JobState.Completed; 
        }
        else
        {
            job.Status = JobState.Processing; 
        }
    }

    public void setOutputFile(string jobId, string outputType, FileMetadata fileMeta){

        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        job.Files[outputType] = fileMeta;

        _jobs[job.JobId] = job;
    }

    public FileMetadata getOutputFile(string jobId, string outputType){
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        if (!job.Files.TryGetValue(outputType, out var fileMeta))
            throw new Exception("Output type not found");

        return fileMeta;
    }


}