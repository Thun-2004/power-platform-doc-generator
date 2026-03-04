
namespace backend.Api.DTO;

public class ResponseModel<T>
{
    public required int Status { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}