namespace backend.Application.DTO;

public record JobOutputResult(
    byte[] Content,
    string MimeType,
    string DownloadName
);