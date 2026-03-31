using backend.Api.Controllers;
using backend.Api.DTO;
using backend.Application.DTO;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Tests;

public class FileControllerTests
{
    [Fact]
    public async Task Generate_WhenFileMissing_ReturnsBadRequest()
    {
        var controller = new FileController(
            new StubUploadService(),
            new StubJobStatusService(),
            new StubJobOutputService());

        var req = new UploadRequest
        {
            File = null!,
            LlmModel = "gpt-4.1",
        };

        var result = await controller.Generate(req, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File is required", bad.Value);
    }

    [Fact]
    public async Task Generate_WhenFileEmpty_ReturnsBadRequest()
    {
        var controller = new FileController(
            new StubUploadService(),
            new StubJobStatusService(),
            new StubJobOutputService());

        var req = new UploadRequest
        {
            File = new FormFile(new MemoryStream(), 0, 0, "file", "empty.zip"),
            LlmModel = "gpt-4.1",
        };

        var result = await controller.Generate(req, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private sealed class StubUploadService : IUploadService
    {
        public Task<JobStartResult> StartJobAsync(
            IFormFile file,
            List<string> outputTypes,
            string LlmModel,
            IReadOnlyDictionary<string, string>? outputPrompts,
            CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<JobStartResult> RegenerateJobAsync(
            string jobId,
            List<string> outputTypes,
            string llmModel,
            IReadOnlyDictionary<string, string>? outputPrompts,
            CancellationToken ct) =>
            throw new NotImplementedException();
    }

    private sealed class StubJobStatusService : IJobStatusService
    {
        public JobStatusResult GetJobStatus(string jobId) =>
            throw new NotImplementedException();
    }

    private sealed class StubJobOutputService : IJobOutputService
    {
        public Task<JobOutputResult> GetJobOutputAsync(string jobId, string outputType, CancellationToken ct) =>
            throw new NotImplementedException();
    }
}
