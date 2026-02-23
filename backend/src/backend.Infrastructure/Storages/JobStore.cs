using backend.Domain; 
using backend.Application.Interfaces;
using System.Collections.Concurrent;

namespace backend.Infrastructure.Storages; 


public class JobStore : IJobStore
{

    //link job id to job for fast look up : O(1)
    private readonly ConcurrentDictionary<string, JobRecord> _jobs = new(); 

    public JobRecord Create(List<string> outputTypes, string zipFilePath)
    {

        var map = new Dictionary<string, FileMetadata>();
        foreach (var outType in outputTypes)
        {
            map[outType] = new FileMetadata
            {
                Status = JobState.Pending,
                Type = outType,
                FilePath = null,
                MimeType = Utility.outputTypeToMimeTypeConverter(outType)
            };
        }

        var job = new JobRecord
        {
            JobId = Guid.NewGuid().ToString(),
            JobStatus = JobState.Pending,
            ZipFilePath = zipFilePath, 
            OutputType_FileMeta_Matches = map
        
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
        _jobs[job.JobId] = job;
    }

    public void setJobZipFilePath(string jobId, string filePath)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        job.ZipFilePath = filePath;

        _jobs[job.JobId] = job;
    }

    public string getJobFilePath(string jobId)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        return job.ZipFilePath;
    }

    public JobState GetJobProgress(string jobId)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        return job.JobStatus;
    }

    public JobState GetSingleOutputFileProgressByType(string jobId, string outputType)
    {
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        if (!job.OutputType_FileMeta_Matches.TryGetValue(outputType, out var fileMeta))
            throw new Exception("Output type not found");

        return fileMeta.Status;
    }

    public void UpdateSingleOutputFileProgress(string jobId, string outputType, JobState outputfileProgress)
    {
        //use lock
        JobRecord job;
        if (!_jobs.TryGetValue(jobId, out job)){
            Console.WriteLine("Job not found");
            return;
        }

        if (!job.OutputType_FileMeta_Matches.TryGetValue(outputType, out var fileMeta)){
            Console.WriteLine("Output type not found");
            throw new Exception("Output type not found");
        }

        fileMeta.Status = outputfileProgress; 
        Console.WriteLine($"Updated output type {outputType} to status {fileMeta.Status}");

        List<JobState> allProgress = job.OutputType_FileMeta_Matches.Values.Select(meta => meta.Status).ToList();

        if (allProgress.Any(p => p == JobState.Failed))
        {
            job.JobStatus = JobState.Failed; 
        }else if (allProgress.All(p => p == JobState.Completed))
        {
            job.JobStatus = JobState.Completed; 
        }
        else
        {
            job.JobStatus = JobState.Processing; 
        }
    }

    public void setOutputFile(string jobId, string outputType, string filepath){

        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        job.OutputType_FileMeta_Matches[outputType].FilePath = filepath;

        _jobs[job.JobId] = job;
    }

    public FileMetadata getOutputFile(string jobId, string outputType){
        JobRecord job; 
        if (!_jobs.TryGetValue(jobId, out job))
            throw new Exception("Job not found");

        if (!job.OutputType_FileMeta_Matches.TryGetValue(outputType, out var fileMeta))
            throw new Exception("Output type not found");

        if (fileMeta == null)
            throw new Exception("Output not yet avaiable"); 

        return fileMeta;
    }
}