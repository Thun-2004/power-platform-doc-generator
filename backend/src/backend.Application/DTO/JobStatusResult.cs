namespace backend.Application.DTO;

public record JobStatusResult(
    string JobId,
    string JobStatus,
    IReadOnlyDictionary<string, string> Progress
    string? Error,
    List<string>? FailedOutputType

);