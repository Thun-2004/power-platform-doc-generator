namespace backend.Application.Config;

// Default values
public class LlmOptions
{
    public const string SectionName = "Llm";

    public string DefaultModel { get; set; } = "gpt-4.1";
    public string VectorStoreName { get; set; } = "company_app-vs";
    public int TimeoutMinutes { get; set; } = 10;
    public int UploadMaxConcurrency { get; set; } = 5;

    public Dictionary<string, string> LLMUrls { get; set; } = new()
    {
        ["openai"] = "https://uofg-team-project-sh38-resource.openai.azure.com/openai/v1/",
        ["claude"] = "https://api.anthropic.com/v1/"
    };

    public Dictionary<string, string> LLMKeys { get; set; } = new()
    {
        ["openai"] = "AZURE_OPENAI_API_KEY",
        ["claude"] = "ANTHROPIC_API_KEY"
    }; 
}
