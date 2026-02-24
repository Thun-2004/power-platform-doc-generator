// using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace backend.Domain;

public class UploadedFile
{
    public required string OriginalName { get; set; }
    public required string StoredPath { get; set; }
}
