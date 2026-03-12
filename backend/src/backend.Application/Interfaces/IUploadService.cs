using backend.Application.DTO;
using Microsoft.AspNetCore.Http;

namespace backend.Application.Interfaces;
public interface IUploadService
{
    Task<JobStartResult> StartJobAsync(IFormFile file, List<string> outputTypes, string LlmModel, IReadOnlyDictionary<string, string>? outputPrompts, CancellationToken ct); 
}