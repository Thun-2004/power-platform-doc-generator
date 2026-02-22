namespace backend.Application.DTO;

public record JobStartResult(
    string JobId,
    Dictionary<string, string> OutputFilesMetas
);