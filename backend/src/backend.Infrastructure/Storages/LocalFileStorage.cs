using Microsoft.AspNetCore.Http;
using backend.Application.Interfaces;
using backend.Application.LLM; // if FileProcessing is in backend.Application

namespace backend.Infrastructure.Storage;

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

        // dirs
        string rawinputDir = Path.Combine(Environment.CurrentDirectory, "TestFiles");
        if (!Directory.Exists(rawinputDir))
            Directory.CreateDirectory(rawinputDir);

        string ragoutDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");
        if (!Directory.Exists(ragoutDir))
            Directory.CreateDirectory(ragoutDir);

        string parsedDir = Path.Combine(Directory.GetCurrentDirectory(), "parsed_outputs");
        if (!Directory.Exists(parsedDir))
            Directory.CreateDirectory(parsedDir);

        // unique file path
        string fullFilePath = _fileProcessing.CreateFile(originalFileName, ext, rawinputDir);

        await using var stream = System.IO.File.Create(fullFilePath);
        await file.CopyToAsync(stream, ct);

        return fullFilePath;
    }
}