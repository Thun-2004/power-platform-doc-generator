using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SolutionParserApp;

// ----------------------------
// Models for JSON output
// ----------------------------
public sealed class InventoryEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("bytes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Bytes { get; set; }
}

public sealed class CanvasAppsSection
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("groups")]
    public Dictionary<string, List<string>> Groups { get; set; } = new();
}


public sealed class ModelDrivenAppsSection
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}

public sealed class WorkflowsSection
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("items")]
    public List<Dictionary<string, object>> Items { get; set; } = new();
}

public sealed class EnvVarsSection
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("items")]
    public List<Dictionary<string, object>> Items { get; set; } = new();
}

public sealed class CanvasAppDetail
{
    [JsonPropertyName("app")]
    public string App { get; set; } = "";

    [JsonPropertyName("screens")]
    public List<string> Screens { get; set; } = new();

    [JsonPropertyName("connectors")]
    public List<string> Connectors { get; set; } = new();

    [JsonPropertyName("files_seen")]
    public List<string> FilesSeen { get; set; } = new();
}

public sealed class WorkflowDetail
{
    [JsonPropertyName("workflow")]
    public string Workflow { get; set; } = "";

    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("connectors")]
    public List<string> Connectors { get; set; } = new();

    [JsonPropertyName("env_vars_used")]
    public List<string> EnvVarsUsed { get; set; } = new();

    [JsonPropertyName("trigger")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Trigger { get; set; }

    [JsonPropertyName("purpose")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Purpose { get; set; }

    [JsonPropertyName("actions_detected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ActionsDetected { get; set; }
}

public sealed class RelationshipEdge
{
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("evidence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Evidence { get; set; }
}

public sealed class SolutionReport
{
    [JsonPropertyName("root")]
    public string Root { get; set; } = "";

    [JsonPropertyName("top_level")]
    public List<InventoryEntry> TopLevel { get; set; } = new();

    [JsonPropertyName("canvasapps")]
    public CanvasAppsSection CanvasApps { get; set; } = new();

    // NEW: Model-Driven Apps
    [JsonPropertyName("modeldrivenapps")]
    public ModelDrivenAppsSection ModelDrivenApps { get; set; } = new();

    [JsonPropertyName("workflows")]
    public WorkflowsSection Workflows { get; set; } = new();

    [JsonPropertyName("environmentvariabledefinitions")]
    public EnvVarsSection EnvironmentVariableDefinitions { get; set; } = new();

    [JsonPropertyName("canvasapps_detailed")]
    public List<CanvasAppDetail> CanvasAppsDetailed { get; set; } = new();

    [JsonPropertyName("workflows_detailed")]
    public List<WorkflowDetail> WorkflowsDetailed { get; set; } = new();

    [JsonPropertyName("relationships")]
    public List<RelationshipEdge> Relationships { get; set; } = new();
}