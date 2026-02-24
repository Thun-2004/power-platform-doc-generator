using Microsoft.AspNetCore.Http;

namespace backend.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveUploadAsync(IFormFile file, CancellationToken ct);
}