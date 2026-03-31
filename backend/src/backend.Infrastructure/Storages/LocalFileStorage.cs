using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using backend.Application.Interfaces;
using backend.Application.LLM;
using backend.Application.Config;

namespace backend.Infrastructure.Storages;

public class LocalFileStorage : IFileStorage
{
    private readonly FileProcessing _fileProcessing;
    private readonly FileStorageOptions _options;

    public LocalFileStorage(FileProcessing fileProcessing, IOptions<FileStorageOptions> options)
    {
        _fileProcessing = fileProcessing;
        _options = options.Value;
    }

    // Summary: Saves uploaded folder in appsettings.json to the local file system and returns the full file path.
    public async Task<string> SaveUploadAsync(IFormFile file, CancellationToken ct)
    {
        var originalFileName = file.FileName;
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

        string pacDir = _options.ResolvePacJobsPath();
        if (!Directory.Exists(pacDir))
            Directory.CreateDirectory(pacDir);

        string rawinputDir = _options.ResolveUploadedFilesPath();
        if (!Directory.Exists(rawinputDir))
            Directory.CreateDirectory(rawinputDir);

        string ragoutDir = _options.ResolveRagOutputsPath();
        if (!Directory.Exists(ragoutDir))
            Directory.CreateDirectory(ragoutDir);

        string parsedDir = _options.ResolveParsedOutputsPath();
        if (!Directory.Exists(parsedDir))
            Directory.CreateDirectory(parsedDir);

        string fullFilePath = _fileProcessing.CreateFile(originalFileName, ext, rawinputDir);

        await using var stream = System.IO.File.Create(fullFilePath);
        await file.CopyToAsync(stream, ct);

        return fullFilePath;
    }
}