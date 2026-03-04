namespace backend.Api.DTO; 


public class StatusResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required Dictionary<string, string> Progress { get; set; }
    string? Error,
    List<string>? FailedOutputType
}