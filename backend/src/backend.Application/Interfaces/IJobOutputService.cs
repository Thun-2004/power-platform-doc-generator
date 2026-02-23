using backend.Application.DTO;
using Microsoft.AspNetCore.Http;

namespace backend.Application.Interfaces;
public interface IJobOutputService
{
    Task<JobOutputResult> GetJobOutputAsync(
        string jobId,
        string outputType,
        CancellationToken ct);
}