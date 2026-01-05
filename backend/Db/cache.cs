using backend.Domain; 

public interface IUploadStore {
    List<UploadedFile> Files { get; }
}

public class UploadStore : IUploadStore {
    public List<UploadedFile> Files { get; } = new();
}