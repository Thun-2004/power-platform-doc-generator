// Summary: Orchestrates parsing of a solution export into a structured report of apps, workflows, environment variables, and relationships.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using backend.Application.LLM;
using backend.Application.Helpers;


namespace backend.Application.Parser;

// Summary: Provides the main entry point and helpers for turning a solution folder into JSON and markdown summaries.
public static class SolutionParser
{
    // Summary: Parses the given solution folder, runs canvas unpack if needed, analyzes artifacts, and writes summary outputs.
    public static string Run(string input_path, string output_path, string jobId, string pacJobsDir)
    {
        string input = input_path;
        string output = output_path;

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
        {
            Console.Error.WriteLine("input or output path can not be null");
            return "";
        }

        var root = new DirectoryInfo(Path.GetFullPath(Environment.ExpandEnvironmentVariables(input)));
        if (!root.Exists)
        {
            Console.Error.WriteLine($"Input folder does not exist: {root.FullName}");
            return "";
        }

        var outDirPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(output));
        Directory.CreateDirectory(outDirPath);

        // ------------------------------------------------------------
        // Locate standard folders
        // ------------------------------------------------------------
        var canvasDir = FsHelpers.FindDirCaseInsensitive(root, "CanvasApps");
        var workflowsDir = FsHelpers.FindDirCaseInsensitive(root, "Workflows");
        var envDir = FsHelpers.FindDirCaseInsensitive(root, "environmentvariabledefinitions");
        var canvasSrcDir = FsHelpers.FindDirCaseInsensitive(root, "CanvasAppsSrc");

        // ------------------------------------------------------------
        // Auto-unpack Canvas Apps (once per job — duplicate blocks were
        // running pac 3× and dominated parse time).
        // Copy ONLY the inner CanvasAppsSrc folder from the temp dir.
        // ------------------------------------------------------------
        if (canvasDir != null && canvasDir.Exists)
        {
            var msappFiles = Directory.EnumerateFiles(canvasDir.FullName, "*.msapp", SearchOption.TopDirectoryOnly).ToList();

            if (msappFiles.Count > 0)
            {
                Console.WriteLine($"Found {msappFiles.Count} .msapp file(s) — running pac canvas unpack...");

                var newPacFolderDir = Path.Combine(pacJobsDir, jobId);
                var newPacSolutionFileDir = Path.Combine(root.FullName, "CanvasAppsSrc");

                FsHelpers.RemoveDirectory(newPacFolderDir);
                Directory.CreateDirectory(newPacFolderDir);

                FsHelpers.RemoveDirectory(newPacSolutionFileDir);
                Directory.CreateDirectory(newPacSolutionFileDir);

                foreach (var msappPath in msappFiles)
                {
                    try
                    {
                        Exporting.RunProcess(
                            "pac",
                            $"canvas unpack --msapp \"{msappPath}\" --sources CanvasAppsSrc",
                            newPacFolderDir,
                            isPac: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARN] pac unpack failed for {Path.GetFileName(msappPath)}: {ex.Message}");
                    }
                }

                var unpackedCanvasSrc = Path.Combine(newPacFolderDir, "CanvasAppsSrc");
                if (Directory.Exists(unpackedCanvasSrc))
                    FsHelpers.CopyDirectory(unpackedCanvasSrc, newPacSolutionFileDir);

                FsHelpers.RemoveDirectory(newPacFolderDir);

                canvasSrcDir = new DirectoryInfo(newPacSolutionFileDir);
            }
        }

        // ------------------------------------------------------------
        // Detect model-driven apps
        // ------------------------------------------------------------
        var modelDrivenAppNames = ModelDrivenAppsParsing.DetectModelDrivenApps(root);

        // ------------------------------------------------------------
        // Build report
        // ------------------------------------------------------------
        var report = new SolutionReport
        {
            Root = root.FullName,
            TopLevel = TopLevelInventory(root),
            CanvasApps = new CanvasAppsSection
            {
                Exists = canvasDir != null,
                Groups = canvasDir != null
                    ? CanvasAppsParsing.GroupCanvasApps(canvasDir)
                    : new Dictionary<string, List<string>>()
            },
            ModelDrivenApps = new ModelDrivenAppsSection
            {
                Exists = modelDrivenAppNames.Count > 0,
                Items = modelDrivenAppNames
            },
            Workflows = new WorkflowsSection
            {
                Exists = workflowsDir != null,
                Items = workflowsDir != null
                    ? ListFiles(workflowsDir, ".json")
                    : new List<Dictionary<string, object>>()
            },
            EnvironmentVariableDefinitions = new EnvVarsSection
            {
                Exists = envDir != null,
                Items = envDir != null
                    ? ListDirs(envDir)
                    : new List<Dictionary<string, object>>()
            }
        };

        var envVarNames = report.EnvironmentVariableDefinitions.Items
            .Select(x => x.TryGetValue("name", out var v) ? v?.ToString() ?? "" : "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (canvasDir != null || (canvasSrcDir != null && canvasSrcDir.Exists))
            report.CanvasAppsDetailed = CanvasAppsParsing.ParseCanvasAppsDetailed(canvasDir ?? root, canvasSrcDir);

        if (workflowsDir != null)
            report.WorkflowsDetailed = WorkflowsParsing.ParseWorkflowsDetailed(workflowsDir, envVarNames);

        report.Relationships = Relationships.BuildRelationships(report, envVarNames, canvasDir, canvasSrcDir);

        int canvasGroupsCount = report.CanvasApps.Groups.Count;
        int modelDrivenCount = report.ModelDrivenApps.Items.Count;
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
        md.AppendLine($"- Model-Driven Apps: **{modelDrivenCount}**");
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

        md.AppendLine("## Model-Driven Apps");
        if (modelDrivenCount == 0) md.AppendLine("None found");
        else
        {
            foreach (var app in report.ModelDrivenApps.Items)
                md.AppendLine($"- {app}");
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
                modeldrivenapps = modelDrivenCount,
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
        File.WriteAllText(Path.Combine(chunksDir, "modeldrivenapps.json"), JsonSerializer.Serialize(report.ModelDrivenApps, jsonOptions), Encoding.UTF8);
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
        Console.WriteLine($"Model-Driven Apps: {modelDrivenCount}");
        Console.WriteLine($"Workflows: {workflowsCount}");
        Console.WriteLine($"Environment variables: {envCount}");
        Console.WriteLine($"Screens found: {screenCount}");
        Console.WriteLine($"Relationship edges inferred: {relCount}");
        Console.WriteLine($"Reports written to: {outDirPath}");
        Console.WriteLine($"Chunks written to: {chunksDir}");

        return chunksDir;
    }

 
    static List<InventoryEntry> TopLevelInventory(DirectoryInfo root)
    {
        var inv = new List<InventoryEntry>();
        foreach (var item in FsHelpers.SafeListDir(root))
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
        foreach (var item in FsHelpers.SafeListDir(folder))
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
        foreach (var item in FsHelpers.SafeListDir(folder))
            if (item is DirectoryInfo d)
                outList.Add(new Dictionary<string, object> { ["name"] = d.Name + "/" });
        return outList;
    }
}