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
        FileDescriptor fileDescriptor = CreateFileDescriptor(jobId, outputType,  filePath);
        var bytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);

        return new JobOutputResult(
            Content: bytes,
            MimeType: string.IsNullOrWhiteSpace(fileMetadata.MimeType) ? fileDescriptor.MimeType : fileMetadata.MimeType,
            DownloadName: fileDescriptor.DownloadName
        ); 
    }


    private FileDescriptor CreateFileDescriptor(string jobId, string outputType,  string path)
    {

        string originalfileName = _jobs.GetUploadedFileName(jobId); 
        string ext = Path.GetExtension(path);

        if (string.IsNullOrEmpty(ext))
        {
            return new FileDescriptor("application/octet-stream", $"{originalfileName}_{ext}.bin");
        }

        if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"{originalfileName}_{outputType}.docx");
        }

        if (ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{originalfileName}_{outputType}.xlsx");
        }

        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor("application/pdf", $"{originalfileName}_{outputType}.pdf");
        }

        if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new FileDescriptor("application/zip", $"{originalfileName}_{outputType}.zip");
        }

        return new FileDescriptor("application/octet-stream", $"{originalfileName}_{outputType}");
    }
}
