
namespace backend.Api.DTO; 

public class RegenerateRequest
{
    public string jobId { get; set; } = default!;
    public List<string> SelectedOutputTypes { get; set; } = new();
    public string LlmModel { get; set; } = "gpt-4.1";
}