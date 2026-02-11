
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
    public required string JobId {get; set; } = ""; 
    public required JobState JobStatus {get; set; } = JobState.Pending; 
    public string ZipFilePath { get; set; } = "";
    public Dictionary<string, FileMetadata?> OutputType_FileMeta_Matches { get; set; } = new(); 

}

public class JobResponse 
{
    public required string JobId { get; set; }
    public required string JobStatus { get; set; }
    public required string JobStatusUrl { get; set; }
    public Dictionary<string, string>? OutputFilesMetas { get; set; }
}

public class FileMetadata
{
    public required JobState Status { get; set; }
    public required string Type { get; set; }
    // public int Byte { get; set; } = 0;

    public string? FilePath { get; set; }
    
    // public required string DownloadUrl { get; set; }
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



