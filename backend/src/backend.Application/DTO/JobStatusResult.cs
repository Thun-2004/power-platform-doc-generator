namespace backend.Application.DTO;

public record JobStatusResult(
    string JobId,
    string JobStatus,
    IReadOnlyDictionary<string, string> Progress,
    IReadOnlyDictionary<string, string> Errors
); 