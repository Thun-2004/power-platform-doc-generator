
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


public class JobRecord
{
    public string JobId {get; set; } = ""; 
    public JobState Status {get; set; } = JobState.Pending; 

    public Dictionary<string, JobState> Progress { get; set; } = new(); 

    public Dictionary<string, FileMetadata> Files { get; set; } = new(); 

}

public class JobResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required string JobStatusUrl { get; set; }
    public List<FileMetadata>? Files { get; set; }
}

public class FileMetadata
{
    public required string Type { get; set; }
    public string FileName { get; set; } = "";
    public required string DownloadUrl { get; set; }
    public string MimeType { get; set; } = "";
}

public class StatusResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required Dictionary<string, string> Progress { get; set; }
}

// public class UploadRequest
// {
//     public IFormFile File { get; set; } = default!;
//     public List<string> SelectedOutputTypes { get; set; } = new();
// }



