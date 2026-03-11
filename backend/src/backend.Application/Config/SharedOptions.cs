namespace backend.Application.Config;


//TODO: use those variables in the code
public class SharedOptions
{
    public const string SectionName = "Shared";

    public string FrontendUrl { get; set; } = "http://localhost:5173";
    public string BackendUrl { get; set; } = "http://localhost:5280";

    public string[] CorsOrigins { get; set; } = ["http://localhost:5173", "https://client.scalar.com"];

    public string[] AllowedOutputFileTypes { get; set; } =
        ["overview", "workflows", "faq", "diagrams", "erd", "screen-mapping", "environment-variables"];

    public string[] AllowedUploadedFileTypes { get; set; } = [".zip", ".pdf"];
    public string[] AllowedExportTypes { get; set; } = [".docx", ".xlxs"];

    public Dictionary<string, string[]> AIModels { get; set; } = new()
    {
        ["openai"] = ["gpt-4.1"],
        ["claude"] = ["claude-4.6-sonnet"]
    };
}
