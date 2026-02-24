using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable

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
        var canvasSrcDir = FindDirCaseInsensitive(root, "CanvasAppsSrc"); // you created this with pac canvas unpack
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

        var envVarNames = report.EnvironmentVariableDefinitions.Items
            .Select(x => x.TryGetValue("name", out var v) ? v?.ToString() ?? "" : "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (canvasDir != null)
            report.CanvasAppsDetailed = ParseCanvasAppsDetailed(canvasDir, canvasSrcDir);

        if (workflowsDir != null)
            report.WorkflowsDetailed = ParseWorkflowsDetailed(workflowsDir, envVarNames);

        report.Relationships = BuildRelationships(report, envVarNames, canvasDir, canvasSrcDir);

        int canvasGroupsCount = report.CanvasApps.Groups.Count;
        int workflowsCount = report.Workflows.Items.Count;
        int envCount = report.EnvironmentVariableDefinitions.Items.Count;

        int screenCount = report.CanvasAppsDetailed.Sum(a => a.Screens.Count);
        int appConnectorCount = report.CanvasAppsDetailed.Sum(a => a.Connectors.Count);
        int flowConnectorCount = report.WorkflowsDetailed.Sum(w => w.Connectors.Count);
        int relCount = report.Relationships.Count;

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
        md.AppendLine($"- Screens found (Canvas Apps): **{screenCount}**");
        md.AppendLine($"- App connectors found: **{appConnectorCount}**");
        md.AppendLine($"- Workflow connectors found: **{flowConnectorCount}**");
        md.AppendLine($"- Relationship edges inferred: **{relCount}**");
        md.AppendLine();

        md.AppendLine("## Canvas Apps (grouped)");
        if (canvasGroupsCount == 0) md.AppendLine("None found (CanvasApps folder missing or empty)");
        else
        {
            foreach (var kvp in report.CanvasApps.Groups)
                md.AppendLine($"- {kvp.Key}");
        }

        md.AppendLine();
        md.AppendLine("## Workflows");
        if (workflowsCount == 0) md.AppendLine("None found (Workflows folder missing or empty)");
        else
        {
            foreach (var wf in report.Workflows.Items)
            {
                var name = wf["name"]?.ToString() ?? "";
                md.AppendLine($"- {name}");
            }
        }

        md.AppendLine();
        md.AppendLine("## Environment Variable Definitions");
        if (envCount == 0) md.AppendLine("None found (environmentvariabledefinitions missing or empty)");
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
            counts = new
            {
                canvasapps_groups = canvasGroupsCount,
                workflows = workflowsCount,
                envvars = envCount,
                screens = screenCount,
                app_connectors = appConnectorCount,
                workflow_connectors = flowConnectorCount,
                relationships = relCount
            },
            top_level = report.TopLevel
        };

        File.WriteAllText(Path.Combine(chunksDir, "overview.json"), JsonSerializer.Serialize(overviewObj, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "canvasapps.json"), JsonSerializer.Serialize(report.CanvasApps, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "envvars.json"), JsonSerializer.Serialize(report.EnvironmentVariableDefinitions, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "workflows.json"), JsonSerializer.Serialize(report.Workflows, jsonOptions), Encoding.UTF8);

        File.WriteAllText(Path.Combine(chunksDir, "canvasapps_detailed.json"), JsonSerializer.Serialize(report.CanvasAppsDetailed, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "workflows_detailed.json"), JsonSerializer.Serialize(report.WorkflowsDetailed, jsonOptions), Encoding.UTF8);
        File.WriteAllText(Path.Combine(chunksDir, "relationships.json"), JsonSerializer.Serialize(report.Relationships, jsonOptions), Encoding.UTF8);

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
        Console.WriteLine($"Screens found: {screenCount}");
        Console.WriteLine($"Relationship edges inferred: {relCount}");
        Console.WriteLine($"Reports written to: {outDirPath}");
        Console.WriteLine($"Chunks written to: {chunksDir}");

        return chunksDir;
    }

    // ----------------------------
    // Detailed parsing helpers
    // ----------------------------
    static List<CanvasAppDetail> ParseCanvasAppsDetailed(DirectoryInfo canvasDir, DirectoryInfo? canvasSrcDir)
    {
        var result = new List<CanvasAppDetail>();

        // Shape A: old export: CanvasApps contains files (msapp, identity, etc)
        // Shape B: pac solution unpack: CanvasApps contains files including *.msapp, and meta
        // Shape C: pac canvas unpack: CanvasAppsSrc/<AppName>/Src/*.fx.yaml plus Connections.json

        // Build list of apps we know about from CanvasApps (file based)
        var appNamesFromCanvasApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in SafeListDir(canvasDir))
        {
            if (item is not FileInfo f) continue;
            if (f.Name.EndsWith("_DocumentUri.msapp", StringComparison.OrdinalIgnoreCase))
            {
                var baseName = f.Name.Substring(0, f.Name.Length - "_DocumentUri.msapp".Length);
                appNamesFromCanvasApps.Add(baseName);
            }
        }

        // Prefer CanvasAppsSrc for screens and formulas if it exists
        if (canvasSrcDir != null && canvasSrcDir.Exists)
        {
            foreach (var appFolder in SafeListDir(canvasSrcDir).OfType<DirectoryInfo>())
            {
                var detail = new CanvasAppDetail { App = appFolder.Name };

                var srcDir = FindDirCaseInsensitive(appFolder, "Src");
                if (srcDir != null)
                {
                    var screenFiles = srcDir.EnumerateFiles("*.fx.yaml", SearchOption.TopDirectoryOnly)
                        .Where(f => !IsIgnored(f.Name))
                        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var f in screenFiles)
                    {
                        var screenName = Path.GetFileNameWithoutExtension(f.Name);
                        detail.Screens.Add(screenName);
                        detail.FilesSeen.Add(RelPath(appFolder, f.FullName));
                    }
                }

                // Connectors often exist in Connections.json in canvas unpack output
                var connectionsJson = appFolder.GetFiles("Connections.json", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Name.Equals("Connections.json", StringComparison.OrdinalIgnoreCase));

                if (connectionsJson != null)
                {
                    detail.FilesSeen.Add(RelPath(appFolder, connectionsJson.FullName));
                    foreach (var c in ExtractConnectorNamesFromConnectionsJson(connectionsJson.FullName))
                        detail.Connectors.Add(c);
                }

                detail.Screens = detail.Screens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                detail.Connectors = detail.Connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                detail.FilesSeen = detail.FilesSeen.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                result.Add(detail);
            }
        }

        // If CanvasAppsSrc was not present or had no folders, still emit app entries from CanvasApps
        if (result.Count == 0)
        {
            foreach (var appName in appNamesFromCanvasApps.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                result.Add(new CanvasAppDetail { App = appName });
        }
        else
        {
            // Ensure apps that exist in CanvasApps but not unpacked are still represented
            var existing = result.Select(r => r.App).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var appName in appNamesFromCanvasApps)
            {
                if (!existing.Contains(appName))
                    result.Add(new CanvasAppDetail { App = appName });
            }
        }

        return result.OrderBy(x => x.App, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static List<string> ExtractConnectorNamesFromConnectionsJson(string path)
    {
        var text = SafeReadAllText(path);
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(text);
            WalkJson(doc.RootElement, (key, val) =>
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    var s = val.GetString() ?? "";
                    if (LooksLikeConnector(s)) found.Add(NormalizeConnector(s));
                }
            });
        }
        catch
        {
            foreach (Match m in Regex.Matches(text, @"(shared_[a-z0-9]+|/providers/Microsoft\.PowerApps/apis/[a-zA-Z0-9\-_]+)", RegexOptions.IgnoreCase))
                found.Add(NormalizeConnector(m.Value));
        }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static bool LooksLikeConnector(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.StartsWith("shared_", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("/providers/Microsoft.PowerApps/apis/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string NormalizeConnector(string s)
    {
        var t = s.Trim();

        var marker = "/providers/Microsoft.PowerApps/apis/";
        var idx = t.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            t = t.Substring(idx + marker.Length);
            var slash = t.IndexOf('/');
            if (slash >= 0) t = t.Substring(0, slash);
        }

        return t;
    }

    static List<WorkflowDetail> ParseWorkflowsDetailed(DirectoryInfo workflowsDir, HashSet<string> envVarNames)
    {
        var result = new List<WorkflowDetail>();

        // Both formats supported
        var topJson = workflowsDir.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .Where(f => !IsIgnored(f.Name))
            .ToList();

        foreach (var f in topJson)
        {
            var wf = new WorkflowDetail
            {
                Workflow = Path.GetFileNameWithoutExtension(f.Name),
                File = f.Name
            };

            var text = SafeReadAllText(f.FullName);
            wf.Connectors = ExtractConnectorsFromFlowJson(text);
            wf.EnvVarsUsed = ExtractEnvVarsFromText(text, envVarNames);

            wf.Connectors = wf.Connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.EnvVarsUsed = wf.EnvVarsUsed.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            result.Add(wf);
        }

        var wfFolders = SafeListDir(workflowsDir).OfType<DirectoryInfo>().ToList();
        foreach (var folder in wfFolders)
        {
            var def = folder.EnumerateFiles("*.json", SearchOption.AllDirectories)
                .FirstOrDefault(x => x.Name.Equals("definition.json", StringComparison.OrdinalIgnoreCase))
                ?? folder.EnumerateFiles("*.json", SearchOption.AllDirectories).FirstOrDefault();

            if (def == null) continue;

            var wf = new WorkflowDetail
            {
                Workflow = folder.Name,
                File = RelPath(workflowsDir, def.FullName)
            };

            var text = SafeReadAllText(def.FullName);
            wf.Connectors = ExtractConnectorsFromFlowJson(text);
            wf.EnvVarsUsed = ExtractEnvVarsFromText(text, envVarNames);

            wf.Connectors = wf.Connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            wf.EnvVarsUsed = wf.EnvVarsUsed.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            result.Add(wf);
        }

        return result
            .GroupBy(x => x.Workflow, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.Workflow, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    static List<string> ExtractConnectorsFromFlowJson(string jsonText)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(jsonText)) return new List<string>();

        foreach (Match m in Regex.Matches(jsonText, @"shared_[a-z0-9]+", RegexOptions.IgnoreCase))
            found.Add(m.Value);

        foreach (Match m in Regex.Matches(jsonText, @"/providers/Microsoft\.PowerApps/apis/[a-zA-Z0-9\-_]+", RegexOptions.IgnoreCase))
            found.Add(NormalizeConnector(m.Value));

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static List<string> ExtractEnvVarsFromText(string text, HashSet<string> envVarNames)
    {
        var used = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || envVarNames.Count == 0) return used;

        foreach (var ev in envVarNames)
        {
            if (text.IndexOf(ev, StringComparison.OrdinalIgnoreCase) >= 0)
                used.Add(ev);
        }

        return used;
    }

    // ----------------------------
    // Relationships including careful screen->workflow mapping with evidence
    // ----------------------------
    static List<RelationshipEdge> BuildRelationships(
        SolutionReport report,
        HashSet<string> envVarNames,
        DirectoryInfo? canvasDir,
        DirectoryInfo? canvasSrcDir
    )
    {
        var edges = new List<RelationshipEdge>();

        // App -> Screen and App -> Connector
        foreach (var app in report.CanvasAppsDetailed)
        {
            var appNode = $"app:{app.App}";

            foreach (var s in app.Screens)
            {
                edges.Add(new RelationshipEdge
                {
                    From = appNode,
                    To = $"screen:{app.App}:{s}",
                    Type = "app_to_screen"
                });
            }

            foreach (var c in app.Connectors)
            {
                edges.Add(new RelationshipEdge
                {
                    From = appNode,
                    To = $"connector:{c}",
                    Type = "app_to_connector"
                });
            }
        }

        // Workflow -> EnvVar and Workflow -> Connector
        foreach (var wf in report.WorkflowsDetailed)
        {
            var wfNode = $"workflow:{wf.Workflow}";

            foreach (var c in wf.Connectors)
            {
                edges.Add(new RelationshipEdge
                {
                    From = wfNode,
                    To = $"connector:{c}",
                    Type = "workflow_to_connector"
                });
            }

            foreach (var ev in wf.EnvVarsUsed)
            {
                edges.Add(new RelationshipEdge
                {
                    From = wfNode,
                    To = $"env:{ev}",
                    Type = "workflow_to_env"
                });
            }
        }

        // Careful Screen -> Workflow mapping
        var wfNames = report.WorkflowsDetailed
            .Select(w => w.Workflow)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wfNames.Count > 0)
        {
            // Prefer CanvasAppsSrc because that is where Src/*.fx.yaml lives in your setup
            if (canvasSrcDir != null && canvasSrcDir.Exists)
                AddScreenToWorkflowEdges(canvasSrcDir, wfNames, edges);
        }

        return edges
            .GroupBy(e => $"{e.From}|{e.To}|{e.Type}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    static void AddScreenToWorkflowEdges(
        DirectoryInfo canvasAppsSrcDir,
        List<string> workflowNames,
        List<RelationshipEdge> edges
    )
    {
        var wfSet = new HashSet<string>(
            workflowNames.Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase
        );

        var runCall = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\.Run\s*\(",
            RegexOptions.Compiled);

        foreach (var appFolder in SafeListDir(canvasAppsSrcDir).OfType<DirectoryInfo>())
        {
            var srcDir = FindDirCaseInsensitive(appFolder, "Src");
            if (srcDir == null) continue;

            var screenFiles = srcDir.EnumerateFiles("*.fx.yaml", SearchOption.TopDirectoryOnly)
                .Where(f => !IsIgnored(f.Name))
                .ToList();

            foreach (var f in screenFiles)
            {
                var screenName = Path.GetFileNameWithoutExtension(f.Name);
                var screenNode = $"screen:{appFolder.Name}:{screenName}";

                string[] lines;
                try { lines = File.ReadAllLines(f.FullName, Encoding.UTF8); }
                catch
                {
                    try { lines = File.ReadAllLines(f.FullName); }
                    catch { continue; }
                }

                foreach (var line in lines)
                {
                    var m = runCall.Match(line);
                    if (!m.Success) continue;

                    var called = m.Groups[1].Value.Trim();
                    var match = workflowNames.FirstOrDefault(w =>
                        w.Equals(called, StringComparison.OrdinalIgnoreCase) ||
                        w.StartsWith(called + "-", StringComparison.OrdinalIgnoreCase)
                    );
                    if (match == null) continue;

                    edges.Add(new RelationshipEdge
                    {
                        From = screenNode,
                        To = $"workflow:{match}",
                        Type = "screen_to_workflow",
                        Evidence = $"{f.Name}: {line.Trim()}"
                    });
                }
            }
        }
    }

    // ----------------------------
    // Existing helpers
    // ----------------------------
    static bool IsIgnored(string name) =>
        name.StartsWith(".") || name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);

    static IEnumerable<FileSystemInfo> SafeListDir(DirectoryInfo dir)
    {
        if (!dir.Exists) return Enumerable.Empty<FileSystemInfo>();
        try
        {
            return dir.EnumerateFileSystemInfos()
                .Where(x => !IsIgnored(x.Name))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return Enumerable.Empty<FileSystemInfo>();
        }
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
        // Works for old "files in CanvasApps" format.
        // For pac unpack format, CanvasApps contains folders, and this method returns empty groups — that's ok because CanvasAppsDetailed replaces it.
        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var knownSuffixes = new[]
        {
        "_BackgroundImageUri",
        "_DocumentUri.msapp",
        "_AdditionalUris0_identity.json"
    };

        foreach (var item in SafeListDir(canvasDir))
        {
            // only group files
            if (item is not FileInfo) continue;

            // ignore meta xml (it is not an app file you want to group)
            if (item.Name.EndsWith(".meta.xml", StringComparison.OrdinalIgnoreCase))
                continue;

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


    static string SafeReadAllText(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch
        {
            try { return File.ReadAllText(path); }
            catch { return ""; }
        }
    }

    static void WalkJson(JsonElement el, Action<string?, JsonElement> onValue, string? key = null)
    {
        onValue(key, el);

        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
                WalkJson(p.Value, onValue, p.Name);
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in el.EnumerateArray())
                WalkJson(v, onValue, key);
        }
    }

    static string RelPath(DirectoryInfo baseDir, string fullPath)
    {
        try
        {
            var b = baseDir.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(b, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(b.Length);
        }
        catch { }
        return fullPath;
    }
}