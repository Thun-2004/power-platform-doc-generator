namespace backend.Api.DTO; 


public class StatusResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required Dictionary<string, string> Progress { get; set; } 
    // {<output-type1>: "completed" | "processing" | "failed"...} 
    // See the status of the output types in the job, aka. JobState, in backend/src/backend.Domain/Job.cs
    public Dictionary<string, string>? Errors { get; set; }
}