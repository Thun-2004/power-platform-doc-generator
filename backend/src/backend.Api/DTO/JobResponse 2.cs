

namespace backend.Api.DTO; 
public class JobResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required string JobStatusUrl { get; set; }
    public Dictionary<string, string>? OutputFilesMetas { get; set; }
}