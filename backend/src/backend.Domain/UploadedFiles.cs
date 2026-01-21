// using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace backend.Domain;

public class UploadedFile
{
    public Guid Id { get; set; } = Guid.NewGuid();     
    public required string OriginalName { get; set; }
    public required string StoredPath { get; set; }
    public List<SelectedMode> Modes { get; set; } = []; 
    public FileStatus Status { get; set; } = FileStatus.Uploaded; 
}
