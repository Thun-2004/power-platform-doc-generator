using backend.Domain;

namespace Domain.Tests;

public class FileMetadataTests
{
    [Fact]
    public void FileMetadata_Allows_Null_FilePath_And_ErrorMessage()
    {
        var meta = new FileMetadata
        {
            Status = JobState.Pending,
            Type = "faq",
            FilePath = null,
            MimeType = "",
            ErrorMessage = null,
        };

        Assert.Null(meta.FilePath);
        Assert.Null(meta.ErrorMessage);
        Assert.Equal(string.Empty, meta.MimeType);
    }

    [Fact]
    public void FileMetadata_CanSet_ErrorMessage_WhenFailed()
    {
        var meta = new FileMetadata
        {
            Status = JobState.Failed,
            Type = "diagrams",
            ErrorMessage = "LLM timeout",
        };

        Assert.Equal(JobState.Failed, meta.Status);
        Assert.Equal("LLM timeout", meta.ErrorMessage);
    }
}
