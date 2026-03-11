namespace backend.Application.Config;

public class BackendOptions
{
    public const string SectionName = "Backend";

    public int Timeout { get; set; } = 10;

    public string PromptsBasePath { get; set; } = "../../backend.Application/Prompts";

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

    public string EnvMapPath { get; set; } = "../../backend.Application/LLM/envmap.replybrary.json";
}
