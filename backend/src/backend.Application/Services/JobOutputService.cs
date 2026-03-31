// Summary: Serves generated job output files, computing appropriate MIME types and download names.

using backend.Application.DTO;
using backend.Application.Interfaces;
using backend.Domain;

namespace backend.Application.Services;

// Summary: Application service that loads job output files from storage via the job store.
public class JobOutputService : IJobOutputService
{
    private readonly IJobStore _jobs;
    private readonly record struct FileDescriptor(string MimeType, string DownloadName);


    // Summary: Creates a JobOutputService that uses the job store to locate uploaded and generated files.
    public JobOutputService(IJobStore jobs)
    {
        _jobs = jobs;
    }

    // Summary: Loads the output file for the given job and output type and returns its bytes, MIME type, and download name.
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

    // Summary: Determines the effective MIME type and download filename for an output file based on its extension and original upload name.
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
