using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

#nullable enable


// return SolutionParser.Run(args);

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

public sealed class SolutionReport
{
    [JsonPropertyName("root")]
    public string Root { get; set; } = "";

    [JsonPropertyName("top_level")]
    public List<InventoryEntry> TopLevel { get; set; } = new();

    [JsonPropertyName("canvasapps")]
    public CanvasAppsSection CanvasApps { get; set; } = new();

    [JsonPropertyName("workflows")]
    public WorkflowsSection Workflows { get; set; } = new();

    [JsonPropertyName("environmentvariabledefinitions")]
    public EnvVarsSection EnvironmentVariableDefinitions { get; set; } = new();
}

// ----------------------------
// Parser implementation
// ----------------------------
public static class SolutionParser
{
    public static string Run(string input_path, string output_path)
    {
        string input = input_path;
        string output = output_path;

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("Usage: dotnet run -- --input <solution_folder> --out <output_folder>");
            return "";
        }

        var root = new DirectoryInfo(Path.GetFullPath(Environment.ExpandEnvironmentVariables(input)));
        var outDirPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(output));
        Directory.CreateDirectory(outDirPath);

        var canvasDir = FindDirCaseInsensitive(root, "CanvasApps");
        var workflowsDir = FindDirCaseInsensitive(root, "Workflows");
        var envDir = FindDirCaseInsensitive(root, "environmentvariabledefinitions");

        var report = new SolutionReport
        {
            Root = root.FullName,
            TopLevel = TopLevelInventory(root),
            CanvasApps = new CanvasAppsSection
            {
                Exists = canvasDir != null,
                Groups = canvasDir != null ? GroupCanvasApps(canvasDir) : new Dictionary<string, List<string>>()
            },
            Workflows = new WorkflowsSection
            {
                Exists = workflowsDir != null,
                Items = workflowsDir != null ? ListFiles(workflowsDir, ".json") : new List<Dictionary<string, object>>()
            },
            EnvironmentVariableDefinitions = new EnvVarsSection
            {
                Exists = envDir != null,
                Items = envDir != null ? ListDirs(envDir) : new List<Dictionary<string, object>>()
            }
        };

        int canvasGroupsCount = report.CanvasApps.Groups.Count;
        int workflowsCount = report.Workflows.Items.Count;
        int envCount = report.EnvironmentVariableDefinitions.Items.Count;

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        File.WriteAllText(
            Path.Combine(outDirPath, "solution_report.json"),
            JsonSerializer.Serialize(report, jsonOptions),
            Encoding.UTF8
        );

        var md = new StringBuilder();
        md.AppendLine("# Solution Parse Summary");
        md.AppendLine();
        md.AppendLine($"**Root:** `{root.FullName}`");
        md.AppendLine();
        md.AppendLine("## Key counts");
        md.AppendLine($"- Canvas Apps (grouped): **{canvasGroupsCount}**");
        md.AppendLine($"- Workflows: **{workflowsCount}**");
        md.AppendLine($"- Environment variables: **{envCount}**");
        md.AppendLine();
        md.AppendLine("## Canvas Apps (grouped)");

        if (canvasGroupsCount == 0) md.AppendLine("- None found (CanvasApps folder missing or empty)");
        else
        {
            foreach (var kvp in report.CanvasApps.Groups)
            {
                md.AppendLine($"- **{kvp.Key}**");
                foreach (var part in kvp.Value) md.AppendLine($"  - {part}");
            }
        }

        md.AppendLine();
        md.AppendLine("## Workflows");
        if (workflowsCount == 0) md.AppendLine("- None found (Workflows folder missing or empty)");
        else
        {
            foreach (var wf in report.Workflows.Items)
            {
                var name = wf["name"]?.ToString() ?? "";
                var bytes = wf["bytes"]?.ToString() ?? "";
                md.AppendLine($"- {name} ({bytes} bytes)");
            }
        }

        md.AppendLine();
        md.AppendLine("## Environment Variable Definitions");
        if (envCount == 0) md.AppendLine("- None found (environmentvariabledefinitions missing or empty)");
        else
        {
            foreach (var ev in report.EnvironmentVariableDefinitions.Items)
            {
                var name = ev["name"]?.ToString() ?? "";
                md.AppendLine($"- {name}");
            }
        }

        File.WriteAllText(Path.Combine(outDirPath, "solution_summary.md"), md.ToString(), Encoding.UTF8);

        var chunksDir = Path.Combine(outDirPath, "chunks");
        Directory.CreateDirectory(chunksDir);

        var overviewObj = new
        {
            root = report.Root,
            counts = new { canvasapps_groups = canvasGroupsCount, workflows = workflowsCount, envvars = envCount },
            top_level = report.TopLevel
        };

        File.WriteAllText(Path.Combine(chunksDir, "overview.json"), JsonSerializer.Serialize(overviewObj, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "canvasapps.json"), JsonSerializer.Serialize(report.CanvasApps, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "envvars.json"), JsonSerializer.Serialize(report.EnvironmentVariableDefinitions, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "workflows.json"), JsonSerializer.Serialize(report.Workflows, jsonOptions), Encoding.UTF8);

        var perFlowDir = Path.Combine(chunksDir, "workflows");
        Directory.CreateDirectory(perFlowDir);

        foreach (var wf in report.Workflows.Items)
        {
            var safeName = wf["name"]?.ToString() ?? "unknown.json";
            foreach (var bad in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(bad, '_');

            File.WriteAllText(
                Path.Combine(perFlowDir, $"{safeName}.json"),
                JsonSerializer.Serialize(wf, jsonOptions),
                Encoding.UTF8
            );
        }

        Console.WriteLine("Parsing is complete");
        Console.WriteLine($"Canvas Apps (grouped): {canvasGroupsCount}");
        Console.WriteLine($"Workflows: {workflowsCount}");
        Console.WriteLine($"Environment variables: {envCount}");
        Console.WriteLine($"Reports written to: {outDirPath}");
        Console.WriteLine($"Chunks written to: {chunksDir}");

        return chunksDir;
    }

    static bool IsIgnored(string name) =>
        name.StartsWith(".") || name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);

    static IEnumerable<FileSystemInfo> SafeListDir(DirectoryInfo dir)
    {
        if (!dir.Exists) return Enumerable.Empty<FileSystemInfo>();
        return dir.EnumerateFileSystemInfos()
            .Where(x => !IsIgnored(x.Name))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    static DirectoryInfo? FindDirCaseInsensitive(DirectoryInfo root, string targetName)
    {
        foreach (var item in SafeListDir(root))
            if (item is DirectoryInfo d && d.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return d;
        return null;
    }

    static List<InventoryEntry> TopLevelInventory(DirectoryInfo root)
    {
        var inv = new List<InventoryEntry>();
        foreach (var item in SafeListDir(root))
        {
            if (item is DirectoryInfo)
                inv.Add(new InventoryEntry { Name = item.Name + "/", Type = "dir" });
            else if (item is FileInfo f)
                inv.Add(new InventoryEntry { Name = item.Name, Type = "file", Bytes = f.Length });
        }
        return inv;
    }

    static List<Dictionary<string, object>> ListFiles(DirectoryInfo folder, string? suffix)
    {
        var outList = new List<Dictionary<string, object>>();
        foreach (var item in SafeListDir(folder))
        {
            if (item is not FileInfo f) continue;
            if (suffix != null && !f.Extension.Equals(suffix, StringComparison.OrdinalIgnoreCase)) continue;

            outList.Add(new Dictionary<string, object> { ["name"] = f.Name, ["bytes"] = f.Length });
        }
        return outList;
    }

    static List<Dictionary<string, object>> ListDirs(DirectoryInfo folder)
    {
        var outList = new List<Dictionary<string, object>>();
        foreach (var item in SafeListDir(folder))
            if (item is DirectoryInfo d)
                outList.Add(new Dictionary<string, object> { ["name"] = d.Name + "/" });
        return outList;
    }

    static Dictionary<string, List<string>> GroupCanvasApps(DirectoryInfo canvasDir)
    {
        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var knownSuffixes = new[]
        {
            "_BackgroundImageUri",
            "_DocumentUri.msapp",
            "_AdditionalUris0_identity.json"
        };

        foreach (var item in SafeListDir(canvasDir))
        {
            var name = item.Name;
            var baseName = name;

            foreach (var sfx in knownSuffixes)
            {
                if (name.EndsWith(sfx, StringComparison.Ordinal))
                {
                    baseName = name.Substring(0, name.Length - sfx.Length);
                    break;
                }
            }

            if (!groups.TryGetValue(baseName, out var list))
            {
                list = new List<string>();
                groups[baseName] = list;
            }
            list.Add(name);
        }

        return groups
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase
            );
    }
}