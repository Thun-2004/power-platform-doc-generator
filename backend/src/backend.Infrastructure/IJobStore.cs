
using System.Collections.Concurrent;
using System.Dynamic;


namespace backend.Infrastructure; 


//temp
public class Utility
{
    public static string outputTypeToMimeTypeConverter(string outputType)
    {
        switch (outputType)
        {
            case "ask":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; 

            case "overview":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "workflows":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "faq":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "diagrams":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case "environment-variables":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
              
            default:
                return "application/octet-stream";
        }
    }
}


public interface IJobStore
{
    JobRecord Create(List<string> outputTypes, string zipFilePath);
    JobRecord? Get(string jobId); 
    void Update(JobRecord job); 
    void setJobZipFilePath(string jobId, string filePath); 
    string getJobFilePath(string jobId);
    JobState GetJobProgress(string jobId); 
    JobState GetSingleOutputFileProgressByType(string jobId, string outputType); 
    void UpdateSingleOutputFileProgress(string jobId, string outputType, JobState outputfileProgress); 
     
    void setOutputFile(string jobId, string outputType, string filepath);
    FileMetadata getOutputFile(string jobId, string outputType); 
}

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

        return fileMeta;
    }

    //TODO: set output file path 

    // public void setZipFileInfo(string jobId, List<string> outputTypes, FileMetadata fileMeta)
    // {
    //     JobRecord job; 
    //     if (!_jobs.TryGetValue(jobId, out job))
    //         throw new Exception("Job not found");

    //     foreach (var outputType in outputTypes)
    //     {
    //         job.Files[outputType] = fileMeta;
    //     }

    //     _jobs[job.JobId] = job;
    // }

    // public List<FileMetadata> getZipFileInfo(string jobId, string outputType)
    // {
    //     JobRecord job; 
    //     if (!_jobs.TryGetValue(jobId, out job))
    //         throw new Exception("Job not found");

    //     if (!job.Files.TryGetValue(outputType, out var fileMeta))
    //         throw new Exception("Output type not found");

    //     return fileMeta;
    // }
}