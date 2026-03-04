
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.VisualBasic;

namespace backend.Domain; 

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
    public required string ZipFileName {get; set; } = ""; 
    public required JobState JobStatus {get; set; } = JobState.Pending; 
    public string ZipFilePath { get; set; } = "";
    public Dictionary<string, FileMetadata?> OutputType_FileMeta_Matches { get; set; } = new(); 
    public string? ErrorMessage { get; set; }
    public List<string>? FailedOutputType { get; set; } = []
}

public class FileMetadata
{
    public required JobState Status { get; set; }
    public required string Type { get; set; }
    public string? FilePath { get; set; }
    public string MimeType { get; set; } = "";
}


