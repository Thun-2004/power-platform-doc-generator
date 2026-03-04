using Microsoft.AspNetCore.Http;
using backend.Application.Interfaces;
using backend.Application.LLM;

namespace backend.Infrastructure.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly FileProcessing _fileProcessing;

    public LocalFileStorage(FileProcessing fileProcessing)
    {
        _fileProcessing = fileProcessing;
    }

    public async Task<string> SaveUploadAsync(IFormFile file, CancellationToken ct)
    {
        var originalFileName = file.FileName;
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

        //dirs
        string pacDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "backend.Infrastructure", "FileStorages", "PPCliJobs");
        if (!Directory.Exists(pacDir))
            Directory.CreateDirectory(pacDir);

        string rawinputDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "backend.Infrastructure", "FileStorages", "UploadedFiles");
        if (!Directory.Exists(rawinputDir))
            Directory.CreateDirectory(rawinputDir);

        string ragoutDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "backend.Infrastructure", "FileStorages", "RAGOutputs");
        if (!Directory.Exists(ragoutDir))
            Directory.CreateDirectory(ragoutDir);

        string parsedDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "backend.Infrastructure", "FileStorages", "ParsedOutputs");
        if (!Directory.Exists(parsedDir))
            Directory.CreateDirectory(parsedDir);


        // unique file path
        string fullFilePath = _fileProcessing.CreateFile(originalFileName, ext, rawinputDir);

        await using var stream = System.IO.File.Create(fullFilePath);
        await file.CopyToAsync(stream, ct);

        return fullFilePath;
    }
}