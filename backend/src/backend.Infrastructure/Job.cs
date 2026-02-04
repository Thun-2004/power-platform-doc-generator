
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.VisualBasic;

namespace backend.Infrastructure; 
public enum JobState
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class GeneratedFileMeta
{
    public string Type { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";

}

public class JobRecord
{
    public string JobId {get; set; } = ""; 
    public JobState Status {get; set; } = JobState.Pending; 

    public Dictionary<string, JobState> Progress { get; set; } = new(); 

    public Dictionary<string, GeneratedFileMeta> Files { get; set; } = new(); 

    public JobRecord Create(List<string> outputTypes)
    {
        var job = new JobRecord
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobState.Processing,
            // Progress = outputTypes.ToDictionary(
            //     outtype => 
            // )
        }; 
        return job;
    }

}


