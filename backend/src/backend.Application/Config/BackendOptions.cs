namespace backend.Application.Config;

// Default values
public class BackendOptions
{
    public const string SectionName = "Backend";

    public int Timeout { get; set; } = 10;
    // How long to keep generated/uploaded files before deleting them.
    // Value comes from appsettings.json: Backend:FileStorePeriodInMinutes

    public int FileStorePeriodInMinutes { get; set; } = 5;

    public string PromptsBasePath { get; set; } = "../../backend.Application/Config/Prompts";

    public Dictionary<string, string>? AIPrompts { get; set; }

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

    public string EnvMapPath { get; set; } = "../../backend.Application/LLM/envmap.json"; //temp: should be uploaded one not static
}
