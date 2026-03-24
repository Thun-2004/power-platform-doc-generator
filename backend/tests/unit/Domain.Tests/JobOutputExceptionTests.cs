using backend.Domain;

namespace Domain.Tests;

public class JobOutputExceptionTests
{
    [Fact]
    public void JobOutputNotReadyException_Exposes_JobId_OutputType_Message()
    {
        var ex = new JobOutputNotReadyException("jid-1", "overview", "Still processing");

        Assert.Equal("jid-1", ex.JobId);
        Assert.Equal("overview", ex.OutputType);
        Assert.Equal("Still processing", ex.Message);
        Assert.IsAssignableFrom<JobOutputException>(ex);
    }

    [Fact]
    public void JobOutputFailedException_Exposes_JobId_OutputType_Message()
    {
        var ex = new JobOutputFailedException("jid-2", "erd", "Generation failed");

        Assert.Equal("jid-2", ex.JobId);
        Assert.Equal("erd", ex.OutputType);
        Assert.Equal("Generation failed", ex.Message);
        Assert.IsAssignableFrom<JobOutputException>(ex);
    }
}
