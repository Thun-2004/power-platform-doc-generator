
using backend.Domain; 

namespace backend.Application.Interfaces; 

public interface IJobStore
{
    JobRecord Create(List<string> outputTypes, string zipFileName, string zipFilePath);
    JobRecord? Get(string jobId); 
    string GetUploadedFileName(string jobId); 
    void Update(JobRecord job); 
    void setJobZipFilePath(string jobId, string filePath); 
    string getJobFilePath(string jobId);
    JobState GetJobProgress(string jobId); 
    JobState GetSingleOutputFileProgressByType(string jobId, string outputType); 
    void UpdateSingleOutputFileProgress(string jobId, string outputType, JobState outputfileProgress); 
    void setOutputFile(string jobId, string outputType, string filepath);
    FileMetadata getOutputFile(string jobId, string outputType); 
    void SetOutputError(string jobId, string outputType, string message);
}

