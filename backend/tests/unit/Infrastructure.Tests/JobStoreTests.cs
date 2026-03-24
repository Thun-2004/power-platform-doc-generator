using backend.Domain;
using backend.Infrastructure.Storages;

namespace Infrastructure.Tests;

public class JobStoreTests
{
    [Fact]
    public void Create_AssignsJobId_AndPendingMetadataPerOutputType()
    {
        var store = new JobStore();
        var types = new List<string> { "overview", "faq" };

        var job = store.Create(types, "solution.zip", "/tmp/solution.zip");

        Assert.False(string.IsNullOrWhiteSpace(job.JobId));
        Assert.Equal("solution.zip", job.ZipFileName);
        Assert.Equal("/tmp/solution.zip", job.ZipFilePath);
        Assert.Equal(JobState.Pending, job.JobStatus);
        Assert.Equal(2, job.OutputType_FileMeta_Matches.Count);
        Assert.Equal(JobState.Pending, job.OutputType_FileMeta_Matches["overview"]!.Status);
        Assert.Equal(JobState.Pending, job.OutputType_FileMeta_Matches["faq"]!.Status);
    }

    [Fact]
    public void Get_ReturnsNull_WhenJobIdUnknown()
    {
        var store = new JobStore();

        Assert.Null(store.Get("no-such-id"));
    }

    [Fact]
    public void Get_ReturnsSameJob_AfterCreate()
    {
        var store = new JobStore();
        var created = store.Create(new List<string> { "overview" }, "a.zip", "/a.zip");

        var fetched = store.Get(created.JobId);

        Assert.NotNull(fetched);
        Assert.Same(created, fetched);
    }

    [Fact]
    public void UpdateSingleOutputFileProgress_WhenAllOutputsCompleted_SetsJobStatusCompleted()
    {
        var store = new JobStore();
        var job = store.Create(new List<string> { "overview", "faq" }, "a.zip", "/a.zip");

        store.UpdateSingleOutputFileProgress(job.JobId, "overview", JobState.Completed);
        store.UpdateSingleOutputFileProgress(job.JobId, "faq", JobState.Completed);

        var updated = store.Get(job.JobId);
        Assert.NotNull(updated);
        Assert.Equal(JobState.Completed, updated.JobStatus);
    }

    [Fact]
    public void GetSingleOutputFileProgressByType_ReturnsStatus()
    {
        var store = new JobStore();
        var job = store.Create(new List<string> { "overview" }, "a.zip", "/a.zip");

        var progress = store.GetSingleOutputFileProgressByType(job.JobId, "overview");

        Assert.Equal(JobState.Pending, progress);
    }

    [Fact]
    public void GetOutputFile_ThrowsJobOutputNotReady_WhenStillPending()
    {
        var store = new JobStore();
        var job = store.Create(new List<string> { "overview" }, "a.zip", "/a.zip");

        var ex = Assert.Throws<JobOutputNotReadyException>(() =>
            store.getOutputFile(job.JobId, "overview"));

        Assert.Equal(job.JobId, ex.JobId);
        Assert.Equal("overview", ex.OutputType);
    }
}
