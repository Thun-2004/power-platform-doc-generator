namespace backend.Application.Config;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string BasePath { get; set; } = "..";

    public string UploadedFilesDir { get; set; } = "backend.Infrastructure/FileStorages/UploadedFiles";

    public string ParsedOutputsDir { get; set; } = "backend.Infrastructure/FileStorages/ParsedOutputs";

    public string RagOutputsDir { get; set; } = "backend.Infrastructure/FileStorages/RAGOutputs";

    public string PacJobsDir { get; set; } = "backend.Infrastructure/FileStorages/PPCliJobs";

    public string ResolveUploadedFilesPath() =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, UploadedFilesDir));

    public string ResolveParsedOutputsPath() =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, ParsedOutputsDir));

    public string ResolveRagOutputsPath() =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, RagOutputsDir));

    public string ResolvePacJobsPath() =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, PacJobsDir));

    public string GetJobParsedOutputPath(string jobId) =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, ParsedOutputsDir, jobId));

    public string GetJobRagOutputPath(string jobId) =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), BasePath, RagOutputsDir, jobId));
}
