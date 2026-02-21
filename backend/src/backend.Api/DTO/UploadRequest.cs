
namespace backend.Api.DTO; 

public class UploadRequest
{
    public IFormFile File { get; set; } = default!;
    public List<string> SelectedOutputTypes { get; set; } = new();
}