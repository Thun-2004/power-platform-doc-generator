using backend.Application.DTO;
using backend.Application.Interfaces;
using backend.Domain;

namespace backend.Application.Services;

public class JobOutputService : IJobOutputService
{
    private readonly IJobStore _jobs;
    private readonly record struct FileDescriptor(string MimeType, string DownloadName);


    public JobOutputService(IJobStore jobs)
    {
        _jobs = jobs;
    }

    public async Task<JobOutputResult> GetJobOutputAsync(string jobId, string outputType, CancellationToken ct)
    {
        FileMetadata fileMetadata = _jobs.getOutputFile(jobId, outputType);
        string filePath = fileMetadata.FilePath ?? throw new InvalidOperationException("Output file path is not available.");
        FileDescriptor fileDescriptor = CreateFileDescriptor(filePath);
        var bytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);

        return new JobOutputResult(
            Content: bytes,
            MimeType: string.IsNullOrWhiteSpace(fileMetadata.MimeType) ? fileDescriptor.MimeType : fileMetadata.MimeType,
            DownloadName: fileDescriptor.DownloadName
        ); 
    }

    private static FileDescriptor CreateFileDescriptor(string path)
    {
        string ext = Path.GetExtension(path);

        if (string.IsNullOrEmpty(ext))
        {
            return new FileDescriptor("application/octet-stream", "Generated_Document.bin");
        }

        if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Generated_Document.docx");
        }

        if (ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Generated_Document.xlsx");
        }

        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor("application/pdf", "Generated_Document.pdf");
        }

        if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor("application/zip", "Generated_Document.zip");
        }

        return new FileDescriptor("application/octet-stream", string.Concat("Generated_Document", ext));
    }
}
