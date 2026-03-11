namespace backend.Application.Config;

public class BackendOptions
{
    public const string SectionName = "Backend";

    /// <summary>Request/timeout in minutes.</summary>
    public int Timeout { get; set; } = 10;

    /// <summary>Base path for prompt .txt files, relative to current directory.</summary>
    public string PromptsBasePath { get; set; } = "../../backend.Application/LLM/Prompts";

    /// <summary>Prompt file names (or relative paths) per output type. Key = overview, workflows, faq, etc.</summary>
    public Dictionary<string, string> AIPromptsUrl { get; set; } = new()
    {
        ["overview"] = "overview.txt",
        ["workflows"] = "workflows.txt",
        ["faq"] = "faq.txt",
        ["diagrams"] = "diagrams.txt",
        ["erd"] = "erd.txt",
        ["screen-mapping"] = "screen-mapping.txt",
        ["environment-variables"] = "environment-variables.txt"
    };

    /// <summary>Path to env map JSON (friendly names for env vars), relative to current directory.</summary>
    public string EnvMapPath { get; set; } = "../../backend.Application/LLM/envmap.replybrary.json";

    public Dictionary<string, string> LLMUrls { get; set; } = new()
    {
        ["openai"] = "https://uofg-team-project-sh38-resource.openai.azure.com/openai/v1/",
        ["claude"] = "https://api.anthropic.com/v1/"
    };
}
