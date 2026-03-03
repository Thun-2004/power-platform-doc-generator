using backend.Application.DTO;
using Microsoft.AspNetCore.Http;

namespace backend.Application.Interfaces;
public interface IUploadService
{
    Task<JobStartResult> StartJobAsync(IFormFile file, List<string> outputTypes, bool useLLM, CancellationToken ct); 
}