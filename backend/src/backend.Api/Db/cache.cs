using backend.Domain; 

public interface IUploadStore {
    UploadedFile File { get; }
}

public class UploadStore : IUploadStore {
    public UploadedFile File { get; set; } = default!;
}