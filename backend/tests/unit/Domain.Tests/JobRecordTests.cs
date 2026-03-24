using backend.Domain;

namespace Domain.Tests;

public class JobRecordTests
{
    [Fact]
    public void JobRecord_Defaults_OutputType_FileMeta_Matches_IsEmptyDictionary()
    {
        var job = new JobRecord
        {
            JobId = "job-1",
            ZipFileName = "solution.zip",
            JobStatus = JobState.Pending,
        };

        Assert.NotNull(job.OutputType_FileMeta_Matches);
        Assert.Empty(job.OutputType_FileMeta_Matches);
        Assert.Equal(string.Empty, job.ZipFilePath);
    }

    [Fact]
    public void JobRecord_CanAssign_OutputFiles()
    {
        var meta = new FileMetadata
        {
            Status = JobState.Completed,
            Type = "overview",
            FilePath = "/out/overview.docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };

        var job = new JobRecord
        {
            JobId = "job-1",
            ZipFileName = "solution.zip",
            JobStatus = JobState.Processing,
            OutputType_FileMeta_Matches = new Dictionary<string, FileMetadata?>
            {
                ["overview"] = meta,
            },
        };

        Assert.Single(job.OutputType_FileMeta_Matches);
        Assert.Equal(JobState.Completed, job.OutputType_FileMeta_Matches["overview"]!.Status);
        Assert.Equal("overview", job.OutputType_FileMeta_Matches["overview"]!.Type);
    }
}
