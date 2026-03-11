namespace backend.Application.Config;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Base path for file storage, relative to current directory (e.g. ".." when running from Api bin).</summary>
    public string BasePath { get; set; } = "..";

    /// <summary>Subfolder for uploaded files (e.g. "backend.Infrastructure/FileStorages/UploadedFiles").</summary>
    public string UploadedFilesDir { get; set; } = "backend.Infrastructure/FileStorages/UploadedFiles";

    /// <summary>Subfolder for parsed outputs (e.g. "backend.Infrastructure/FileStorages/ParsedOutputs").</summary>
    public string ParsedOutputsDir { get; set; } = "backend.Infrastructure/FileStorages/ParsedOutputs";

    /// <summary>Subfolder for RAG/generated outputs (e.g. "backend.Infrastructure/FileStorages/RAGOutputs").</summary>
    public string RagOutputsDir { get; set; } = "backend.Infrastructure/FileStorages/RAGOutputs";

    /// <summary>Subfolder for PPCli jobs (e.g. "backend.Infrastructure/FileStorages/PPCliJobs").</summary>
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
