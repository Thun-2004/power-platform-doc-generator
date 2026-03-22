using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using backend.Application.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace backend.Api;

public sealed class FileStorageTtlCleanupService : BackgroundService
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly BackendOptions _backendOptions;

    public FileStorageTtlCleanupService(
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<BackendOptions> backendOptions)
    {
        _fileStorageOptions = fileStorageOptions.Value;
        _backendOptions = backendOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once on startup
        await CleanupOnce(stoppingToken);

        // Then run periodically.
        // every minute check for anything older than TTL.
        var delay = TimeSpan.FromMinutes(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(delay, stoppingToken);
            await CleanupOnce(stoppingToken);
        }
    }

    private Task CleanupOnce(CancellationToken ct)
    {
        var ttlMinutes = _backendOptions.FileStorePeriodInMinutes;
        if (ttlMinutes <= 0) return Task.CompletedTask;

        var cutoffUtc = DateTime.UtcNow - TimeSpan.FromMinutes(ttlMinutes);

        // Uploaded zip files (files directly under the root)
        CleanupOldFiles(_fileStorageOptions.ResolveUploadedFilesPath(), cutoffUtc, ct);

        // Parsed outputs / Rag outputs / PAC jobs are jobId directories.
        CleanupOldDirectories(_fileStorageOptions.ResolveParsedOutputsPath(), cutoffUtc, ct);
        CleanupOldDirectories(_fileStorageOptions.ResolveRagOutputsPath(), cutoffUtc, ct);
        CleanupOldDirectories(_fileStorageOptions.ResolvePacJobsPath(), cutoffUtc, ct);

        return Task.CompletedTask;
    }

    private static void CleanupOldFiles(string? root, DateTime cutoffUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;

        foreach (var file in Directory.EnumerateFiles(root))
        {
            if (ct.IsCancellationRequested) return;
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (lastWrite < cutoffUtc)
                    File.Delete(file);
            }
            catch
            {
                // Best-effort cleanup; ignore individual failures.
            }
        }
    }

    private static void CleanupOldDirectories(string? root, DateTime cutoffUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            if (ct.IsCancellationRequested) return;
            try
            {
                var info = new DirectoryInfo(dir);
                var lastWrite = info.LastWriteTimeUtc;

                if (lastWrite < cutoffUtc)
                    Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup; ignore individual failures.
            }
        }
    }
}

