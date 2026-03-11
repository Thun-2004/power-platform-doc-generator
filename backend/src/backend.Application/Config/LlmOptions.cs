namespace backend.Application.Config;

public class LlmOptions
{
    public const string SectionName = "Llm";

    public string DefaultModel { get; set; } = "gpt-4.1";
    public string VectorStoreName { get; set; } = "replybrary-vs";
    public int TimeoutMinutes { get; set; } = 10;
    public int UploadMaxConcurrency { get; set; } = 5;
}
