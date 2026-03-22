using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RagCliApp;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var apiKey = Env.MustEnv("OPENAI_API_KEY");

            string defaultVs = "vs_6972909f03948191a19a88a9fd13e234";
            string vsId  = defaultVs;
            string model = "gpt-4o-mini";

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            if (args.Length == 0) { Cli.PrintUsage(); return 0; }

            string? GetFlag(string name)
            {
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == name && i + 1 < args.Length)
                        return args[i + 1];
                return null;
            }

            var vsFlag    = GetFlag("--vs");    if (!string.IsNullOrWhiteSpace(vsFlag))    vsId   = vsFlag!;
            var modelFlag = GetFlag("--model"); if (!string.IsNullOrWhiteSpace(modelFlag)) model  = modelFlag!;
            var outFlag   = GetFlag("--out");   if (!string.IsNullOrWhiteSpace(outFlag))   outDir = outFlag!;
            var envMapFlag = GetFlag("--envmap");

            Directory.CreateDirectory(outDir);

            var cmd = args[0].ToLowerInvariant();

            // ──────────────────────────────────────────────────────────────────────
            // index
            // ──────────────────────────────────────────────────────────────────────
            if (cmd == "index")
            {
                var chunks     = GetFlag("--chunks");
                var name       = GetFlag("--name") ?? "solution_chunks";
                var existingVs = GetFlag("--vs");

                if (string.IsNullOrWhiteSpace(chunks) || !Directory.Exists(chunks))
                    throw new Exception("index requires: --chunks \"<path>\"");

                string targetVs;
                if (!string.IsNullOrWhiteSpace(existingVs))
                {
                    targetVs = existingVs!;
                    Console.WriteLine($"Using existing vector store: {targetVs}");
                }
                else
                {
                    Console.WriteLine("Creating vector store...");
                    targetVs = await OpenAIHttp.CreateVectorStore(http, name);
                    Console.WriteLine($"Vector store: {targetVs}");
                }

                var files = Directory.GetFiles(chunks!, "*.json", SearchOption.AllDirectories);
                Console.WriteLine($"Found {files.Length} json files.");
                foreach (var f in files)
                {
                    Console.WriteLine($"Uploading: {Path.GetFileName(f)}");
                    var fileId = await OpenAIHttp.UploadFile(http, f);
                    await OpenAIHttp.AttachFileToVectorStore(http, targetVs, fileId);
                    Console.WriteLine($"  attached file_id={fileId}");
                }
                Console.WriteLine("Done. Vector store id:");
                Console.WriteLine(targetVs);
                return 0;
            }

            // ──────────────────────────────────────────────────────────────────────
            // ask
            // ──────────────────────────────────────────────────────────────────────
            if (cmd == "ask")
            {
                if (args.Length < 2)
                    throw new Exception("ask requires a question: dotnet run -- ask \"...\"");

                var question = string.Join(" ", args.Skip(1));
                var prompt   = PromptRouting.BuildRoutedPrompt(question);
                var answer   = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                Console.WriteLine(answer);
                return 0;
            }

            // ──────────────────────────────────────────────────────────────────────
            // generate
            // ──────────────────────────────────────────────────────────────────────
            if (cmd == "generate")
            {
                if (args.Length < 2)
                    throw new Exception("generate requires a type: overview | workflows | faq | diagrams | erd | screen-mapping");

                var kind = args[1].ToLowerInvariant();

                var envMap     = ProgramLocalHelpers.LoadEnvMap(envMapFlag);
                var envMapText = ProgramLocalHelpers.EnvMapAsBulletText(envMap);

                // ── overview ──────────────────────────────────────────────────────
                if (kind == "overview")
                {
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var canvasDetailedJson    = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var envvarsJson           = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");
                        var relationshipsJson     = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var appsAll  = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new();
                        // ── FIX: no longer filter by wmreply_ prefix ────────────
                        var realApps = appsAll
                            .Where(a => !string.IsNullOrWhiteSpace(a.App)
                                     && !a.App.Equals("CanvasAppsSrc", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        var srcBucket    = appsAll.FirstOrDefault(a => string.Equals(a.App, "CanvasAppsSrc", StringComparison.OrdinalIgnoreCase));
                        var srcScreens   = srcBucket?.Screens    ?? new List<string>();
                        var srcConnectors = srcBucket?.Connectors ?? new List<string>();

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new();
                        var edges     = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)  ?? new();
                        var envNames  = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

                        // Optional model-driven apps chunk
                        var modelDrivenApps = ProgramLocalHelpers.TryReadModelDrivenApps(chunksDir);

                        var screenCount   = srcScreens.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                        var workflowCount = workflows.Count;
                        var realAppCount  = realApps.Count;

                        var edgeCounts = edges
                            .GroupBy(e => e.Type ?? "", StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                        var sb = new StringBuilder();
                        sb.AppendLine("# Solution Overview");
                        sb.AppendLine();
                        sb.AppendLine("This solution package contains Power Platform components integrating with Microsoft 365 and other services.");
                        sb.AppendLine();

                        sb.AppendLine("## Key counts");
                        sb.AppendLine($"- Canvas Apps: {realAppCount}");
                        sb.AppendLine($"- Model-Driven Apps: {modelDrivenApps.Count}");
                        sb.AppendLine($"- Workflows (flows): {workflowCount}");
                        sb.AppendLine($"- Environment variables (referenced): {envNames.Count}");
                        sb.AppendLine($"- Screens (total across canvas apps): {screenCount}");
                        sb.AppendLine();
                        sb.AppendLine("Relationship edges by type:");
                        foreach (var kv in edgeCounts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                            sb.AppendLine($"- {kv.Key}: {kv.Value}");
                        sb.AppendLine();

                        if (modelDrivenApps.Count > 0)
                        {
                            sb.AppendLine("## Model-Driven Apps");
                            foreach (var app in modelDrivenApps)
                                sb.AppendLine($"- {app}");
                            sb.AppendLine();
                        }

                        if (realAppCount > 0)
                        {
                            sb.AppendLine("## Canvas Apps");
                            foreach (var app in realApps.OrderBy(a => a.App, StringComparer.OrdinalIgnoreCase))
                            {
                                var raw  = app.App;
                                var nice = ProgramLocalHelpers.CleanAppDisplay(raw);
                                sb.AppendLine($"- {nice} ({raw})");
                                sb.AppendLine($"  - Screens: {screenCount}");
                                var connList = srcConnectors.Count == 0
                                    ? "Not found in uploaded files"
                                    : string.Join(", ", srcConnectors);
                                sb.AppendLine($"  - Connectors: {connList}");
                                sb.AppendLine();
                            }
                        }
                        else
                        {
                            sb.AppendLine("## Canvas Apps");
                            sb.AppendLine("None found in this solution.");
                            sb.AppendLine();
                        }

                        sb.AppendLine("## Workflows (flows)");
                        foreach (var wf in workflows.OrderBy(w => w.Workflow, StringComparer.OrdinalIgnoreCase))
                        {
                            var nice    = ProgramLocalHelpers.CleanWorkflowDisplay(wf.Workflow);
                            var wfLabel = nice == wf.Workflow ? wf.Workflow : $"{nice} ({wf.Workflow})";
                            sb.AppendLine($"- {wfLabel}");
                        }
                        sb.AppendLine();

                        sb.AppendLine("## Environment Variables");
                        if (envNames.Count == 0) sb.AppendLine("None found.");
                        else
                        {
                            foreach (var ev in envNames)
                                sb.AppendLine($"- {ProgramLocalHelpers.EnvDisplay(ev, envMap)}");
                        }

                        var overviewPath = Path.Combine(outDir, "overview.md");
                        File.WriteAllText(overviewPath, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {overviewPath}");
                        return 0;
                    }

                    // RAG mode
                    var prompt =
@"Generate a clean Markdown solution overview using ONLY the uploaded solution chunks.

You MUST consult:
- overview.json (counts + top level)
- canvasapps.json + canvasapps_detailed.json
- modeldrivenapps.json (if present)
- workflows.json + workflows_detailed.json
- envvars.json
- relationships.json

Rules:
- If a field exists in those files, you MUST use it.
- Only say 'Not found in uploaded files.' if the relevant file truly lacks that data.
- Keep headings and bullet points, client-readable.
- DO NOT assume or hard-code a publisher prefix. Use names as they appear in the files.

Include:
1) Plain-English overview paragraph
2) Counts: canvas apps, model-driven apps, workflows, env vars, screens, relationship edges
3) Model-Driven Apps section (if any)
4) Canvas Apps section (if any): names + screen counts + connectors
5) Workflows section: trigger + purpose + actions_detected
6) Environment variables list

Friendly environment variable names:
" + envMapText;

                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var overviewRagPath = Path.Combine(outDir, "overview.md");
                    File.WriteAllText(overviewRagPath, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {overviewRagPath}");
                    return 0;
                }

                // ── workflows ─────────────────────────────────────────────────────
                if (kind == "workflows")
                {
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var relationshipsJson      = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new();
                        var edges     = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)   ?? new();

                        var screensByWorkflow = edges
                            .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            .GroupBy(e => ProgramLocalHelpers.StripPrefix(e.To, "workflow:"))
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => ProgramLocalHelpers.StripPrefix(e.From, "screen:"))
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
                                StringComparer.OrdinalIgnoreCase
                            );

                        var sb = new StringBuilder();
                        sb.AppendLine("# Workflows");
                        sb.AppendLine();

                        foreach (var wf in workflows.OrderBy(w => w.Workflow, StringComparer.OrdinalIgnoreCase))
                        {
                            var wfDisplay = ProgramLocalHelpers.CleanWorkflowDisplay(wf.Workflow);
                            sb.AppendLine($"## {wfDisplay} ({wf.Workflow})");
                            sb.AppendLine();
                            sb.AppendLine($"- Trigger: {(string.IsNullOrWhiteSpace(wf.Trigger) ? "Not found in uploaded files" : wf.Trigger)}");
                            sb.AppendLine($"- Purpose: {(string.IsNullOrWhiteSpace(wf.Purpose) ? "Not found in uploaded files" : wf.Purpose)}");
                            var actions = wf.ActionsDetected ?? new List<string>();
                            sb.AppendLine($"- Actions detected: {(actions.Count == 0 ? "Not found in uploaded files" : string.Join(", ", actions))}");
                            sb.AppendLine($"- Connectors: {(wf.Connectors.Count == 0 ? "Not found in uploaded files" : string.Join(", ", wf.Connectors))}");
                            var envList = (wf.EnvVarsUsed ?? new List<string>())
                                .Select(v => ProgramLocalHelpers.EnvDisplay(v, envMap))
                                .ToList();
                            sb.AppendLine($"- Environment variables: {(envList.Count == 0 ? "Not found in uploaded files" : string.Join(", ", envList))}");
                            if (screensByWorkflow.TryGetValue(wf.Workflow, out var screens) && screens.Count > 0)
                                sb.AppendLine($"- Invoked from screens: {string.Join(", ", screens)}");
                            else
                                sb.AppendLine("- Invoked from screens: Not found in uploaded files");
                            sb.AppendLine();
                        }

                        var path = Path.Combine(outDir, "workflows.md");
                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    // RAG mode
                    var prompt =
@"Summarise each workflow in Markdown using ONLY the uploaded chunks.
Consult: workflows_detailed.json, relationships.json.
For each workflow include: name, trigger, purpose, actions_detected, connectors, env_vars_used, invoked from screens.
If a field is empty write: Not found in uploaded files.";

                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var aiPath = Path.Combine(outDir, "workflows.md");
                    File.WriteAllText(aiPath, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {aiPath}");
                    return 0;
                }

                // ── faq ───────────────────────────────────────────────────────────
                if (kind == "faq")
                {
                    var prompt =
@"Create a Markdown FAQ for this solution using ONLY the uploaded chunks.
Include ~10 Q&As covering workflows, env vars, canvas apps, model-driven apps, and what the solution does.
If info is missing, say 'Not found in uploaded files.' Keep it concise.";

                    var md   = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var path = Path.Combine(outDir, "faq.md");
                    File.WriteAllText(path, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {path}");
                    return 0;
                }

                // ── diagrams ──────────────────────────────────────────────────────
                if (kind == "diagrams")
                {
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var canvasDetailedJson    = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var envvarsJson           = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

                        var appsAll  = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson)  ?? new();
                        // ── FIX: no wmreply_ filter ─────────────────────────────
                        var apps     = appsAll
                            .Where(a => !string.IsNullOrWhiteSpace(a.App)
                                     && !a.App.Equals("CanvasAppsSrc", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        var workflows       = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new();
                        var envNames        = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);
                        var modelDrivenApps = ProgramLocalHelpers.TryReadModelDrivenApps(chunksDir);

                        var sb = new StringBuilder();
                        sb.AppendLine("flowchart LR");

                        if (modelDrivenApps.Count > 0)
                        {
                            sb.AppendLine("  subgraph ModelDrivenApps");
                            for (int i = 0; i < modelDrivenApps.Count; i++)
                                sb.AppendLine($"    MDA{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(modelDrivenApps[i])}\"]");
                            sb.AppendLine("  end");
                            sb.AppendLine();
                        }

                        if (apps.Count > 0)
                        {
                            sb.AppendLine("  subgraph CanvasApps");
                            for (int i = 0; i < apps.Count; i++)
                            {
                                var raw   = apps[i].App;
                                var nice  = ProgramLocalHelpers.CleanAppDisplay(raw);
                                var label = $"{nice} ({raw})";
                                sb.AppendLine($"    CA{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                            }
                            sb.AppendLine("  end");
                            sb.AppendLine();
                        }

                        sb.AppendLine("  subgraph Workflows");
                        for (int i = 0; i < workflows.Count; i++)
                        {
                            var raw   = workflows[i].Workflow;
                            var nice  = ProgramLocalHelpers.CleanWorkflowDisplay(raw);
                            var label = nice == raw ? raw : $"{nice} ({raw})";
                            sb.AppendLine($"    W{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");
                        sb.AppendLine();
                        sb.AppendLine("  subgraph EnvironmentVariables");
                        sb.AppendLine("    EVH[\"Environment Variables (shared)\"]");
                        for (int i = 0; i < envNames.Count; i++)
                        {
                            var label = ProgramLocalHelpers.EnvDisplay(envNames[i], envMap);
                            sb.AppendLine($"    E{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");
                        sb.AppendLine();

                        sb.AppendLine("  %% Canvas Apps -> PowerApps-triggered Workflows");
                        for (int ca = 0; ca < apps.Count; ca++)
                            for (int w = 0; w < workflows.Count; w++)
                                if (IsPowerAppsTriggered(workflows[w].Trigger))
                                    sb.AppendLine($"  CA{ca + 1} --> W{w + 1}");

                        sb.AppendLine();
                        sb.AppendLine("  %% Workflows -> Env Hub");
                        for (int w = 0; w < workflows.Count; w++)
                            sb.AppendLine($"  W{w + 1} --> EVH");

                        sb.AppendLine();
                        sb.AppendLine("  %% Env Hub -> each env var");
                        for (int e = 0; e < envNames.Count; e++)
                            sb.AppendLine($"  EVH --> E{e + 1}");

                        var path = Path.Combine(outDir, "architecture.mmd");
                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    // RAG mode
                    var ragPrompt =
@"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.
Include all three groups as explicit nodes:
1) Canvas Apps (if any)
2) Model-Driven Apps (if any, from modeldrivenapps.json)
3) Workflows (each as its own node)
4) Environment Variables (each as its own node)
Connect canvas apps -> PowerApps-triggered workflows. Connect all workflows -> EVH hub. EVH hub -> each env var.
Use subgraphs: CanvasApps, ModelDrivenApps, Workflows, EnvironmentVariables.
Use safe IDs: CA1..N, MDA1..N, W1..N, EVH, E1..N.
Output ONLY valid Mermaid code, no markdown fences.
Friendly env var names: " + envMapText;

                    var mermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, ragPrompt);
                    mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();
                    var ragPath = Path.Combine(outDir, "architecture.mmd");
                    File.WriteAllText(ragPath, mermaid, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {ragPath}");
                    return 0;
                }

                // ── erd ───────────────────────────────────────────────────────────
                if (kind == "erd")
                {
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var relationshipsJson     = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var canvasDetailedJson    = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var envvarsJson           = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

                        var edges     = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)  ?? new();
                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new();
                        var appsAll   = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson)   ?? new();
                        
                        var apps = appsAll
                            .Where(a => !string.IsNullOrWhiteSpace(a.App)
                                     && !a.App.Equals("CanvasAppsSrc", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        var envNames        = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);
                        var modelDrivenApps = ProgramLocalHelpers.TryReadModelDrivenApps(chunksDir);
                        var appSetFromChunks = new HashSet<string>(apps.Select(a => a.App), StringComparer.OrdinalIgnoreCase);

                        bool IsAllowedType(string? t) =>
                            t != null && (
                                t.Equals("app_to_screen",          StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("screen_to_workflow",     StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("workflow_to_env",        StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("workflow_to_connector",  StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("app_to_connector",       StringComparison.OrdinalIgnoreCase));

                        var allowedEdges = edges.Where(e => IsAllowedType(e.Type)).ToList();

                        var appNamesUsed       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var screenNamesUsed    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var workflowNamesUsed  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var connectorNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var envNamesUsed       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var e in allowedEdges)
                        {
                            var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
                            var to   = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);
                            switch (e.Type.ToLowerInvariant())
                            {
                                case "app_to_screen":         appNamesUsed.Add(from);      screenNamesUsed.Add(to);    break;
                                case "screen_to_workflow":    screenNamesUsed.Add(from);   workflowNamesUsed.Add(to);  break;
                                case "workflow_to_env":       workflowNamesUsed.Add(from); envNamesUsed.Add(to);       break;
                                case "workflow_to_connector": workflowNamesUsed.Add(from); connectorNamesUsed.Add(to); break;
                                case "app_to_connector":      appNamesUsed.Add(from);      connectorNamesUsed.Add(to); break;
                            }
                        }

                        var appIds  = AssignIds(appNamesUsed,       "CA");
                        var scrIds  = AssignIds(screenNamesUsed,     "S");
                        var wfIds   = AssignIds(workflowNamesUsed,   "W");
                        var envIds  = AssignIds(envNamesUsed,        "E");
                        var connIds = AssignIds(connectorNamesUsed,  "C");

                        var sb = new StringBuilder();
                        sb.AppendLine("flowchart LR");

                        if (modelDrivenApps.Count > 0)
                        {
                            sb.AppendLine("  subgraph ModelDrivenApps");
                            for (int i = 0; i < modelDrivenApps.Count; i++)
                                sb.AppendLine($"    MDA{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(modelDrivenApps[i])}\"]");
                            sb.AppendLine("  end");
                        }

                        if (appIds.Count > 0)
                        {
                            sb.AppendLine("  subgraph CanvasApps");
                            foreach (var app in appIds.Keys)
                            {
                                var nice  = ProgramLocalHelpers.CleanAppDisplay(app);
                                var label = appSetFromChunks.Contains(app) ? $"{nice} ({app})" : nice;
                                sb.AppendLine($"    {appIds[app]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                            }
                            sb.AppendLine("  end");
                        }

                        sb.AppendLine("  subgraph Screens");
                        foreach (var scr in scrIds.Keys)
                            sb.AppendLine($"    {scrIds[scr]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(scr)}\"]");
                        sb.AppendLine("  end");

                        sb.AppendLine("  subgraph Workflows");
                        foreach (var wf in wfIds.Keys)
                        {
                            var nice  = ProgramLocalHelpers.CleanWorkflowDisplay(wf);
                            var label = nice == wf ? wf : $"{nice} ({wf})";
                            sb.AppendLine($"    {wfIds[wf]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");

                        if (connIds.Count > 0)
                        {
                            sb.AppendLine("  subgraph Connectors");
                            foreach (var c in connIds.Keys)
                                sb.AppendLine($"    {connIds[c]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(c)}\"]");
                            sb.AppendLine("  end");
                        }

                        if (envIds.Count > 0)
                        {
                            sb.AppendLine("  subgraph EnvironmentVariables");
                            foreach (var ev in envIds.Keys)
                            {
                                var label = ProgramLocalHelpers.EnvDisplay(ev, envMap);
                                sb.AppendLine($"    {envIds[ev]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                            }
                            sb.AppendLine("  end");
                        }

                        sb.AppendLine();

                        foreach (var e in allowedEdges)
                        {
                            var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
                            var to   = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);
                            switch (e.Type.ToLowerInvariant())
                            {
                                case "app_to_screen":
                                    if (appIds.TryGetValue(from, out var aId) && scrIds.TryGetValue(to, out var sId))
                                        sb.AppendLine($"  {aId} --> {sId}");
                                    break;
                                case "screen_to_workflow":
                                    if (scrIds.TryGetValue(from, out var sId2) && wfIds.TryGetValue(to, out var wId))
                                        sb.AppendLine($"  {sId2} --> {wId}");
                                    break;
                                case "workflow_to_env":
                                    if (wfIds.TryGetValue(from, out var wId2) && envIds.TryGetValue(to, out var eId))
                                        sb.AppendLine($"  {wId2} --> {eId}");
                                    break;
                                case "workflow_to_connector":
                                    if (wfIds.TryGetValue(from, out var wId3) && connIds.TryGetValue(to, out var cId))
                                        sb.AppendLine($"  {wId3} --> {cId}");
                                    break;
                                case "app_to_connector":
                                    if (appIds.TryGetValue(from, out var aId2) && connIds.TryGetValue(to, out var cId2))
                                        sb.AppendLine($"  {aId2} --> {cId2}");
                                    break;
                            }
                        }

                        var path = Path.Combine(outDir, "erd.mmd");
                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    // RAG mode
                    var erdPrompt =
@"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.
Consult: relationships.json, workflows_detailed.json, canvasapps_detailed.json, modeldrivenapps.json, envvars.json.
Use ONLY real edges from relationships.json (app_to_screen, screen_to_workflow, workflow_to_env, workflow_to_connector, app_to_connector).
If modeldrivenapps.json exists and has items, add a ModelDrivenApps subgraph.
IDs: MDA1..N, CA1..N, S1..N, W1..N, E1..N, C1..N.
Output ONLY valid Mermaid code, no markdown fences.
Friendly env var names: " + envMapText;

                    var erdMermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, erdPrompt);
                    erdMermaid = erdMermaid.Replace("```mermaid", "").Replace("```", "").Trim();
                    var erdPath = Path.Combine(outDir, "erd.mmd");
                    File.WriteAllText(erdPath, erdMermaid, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {erdPath}");
                    return 0;
                }

                // ── screen-mapping ────────────────────────────────────────────────
                if (kind == "screen-mapping")
                {
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var relationshipsJson      = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new();
                        var wfByName  = workflows.ToDictionary(w => w.Workflow, w => w, StringComparer.OrdinalIgnoreCase);
                        var edges     = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)   ?? new();

                        var rows = edges
                            .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            .Select(e =>
                            {
                                var screen   = ProgramLocalHelpers.StripPrefix(e.From, "screen:");
                                var workflow = ProgramLocalHelpers.StripPrefix(e.To,   "workflow:");
                                var wfDisplay = ProgramLocalHelpers.CleanWorkflowDisplay(workflow);
                                var wfOut     = wfDisplay == workflow ? workflow : $"{wfDisplay} ({workflow})";

                                wfByName.TryGetValue(workflow, out var wf);
                                var actions = wf?.ActionsDetected ?? new List<string>();

                                string evidenceFile = "", evidenceSnippet = "";
                                if (!string.IsNullOrWhiteSpace(e.Evidence))
                                {
                                    var idx = e.Evidence.IndexOf(':');
                                    if (idx >= 0) { evidenceFile = e.Evidence[..idx].Trim(); evidenceSnippet = e.Evidence[(idx + 1)..].Trim(); }
                                    else evidenceSnippet = e.Evidence.Trim();
                                }
                                if (evidenceSnippet.Length > 120) evidenceSnippet = evidenceSnippet[..120] + "...";

                                return new
                                {
                                    Screen          = screen,
                                    Workflow        = wfOut,
                                    Trigger         = string.IsNullOrWhiteSpace(wf?.Trigger) ? "Not found in uploaded files" : wf.Trigger,
                                    Purpose         = string.IsNullOrWhiteSpace(wf?.Purpose) ? "Not found in uploaded files" : wf.Purpose,
                                    Actions         = actions.Count == 0 ? "Not found in uploaded files" : string.Join(", ", actions),
                                    EvidenceFile    = string.IsNullOrWhiteSpace(evidenceFile)    ? "Not found in uploaded files" : evidenceFile,
                                    EvidenceSnippet = string.IsNullOrWhiteSpace(evidenceSnippet) ? "Not found in uploaded files" : evidenceSnippet,
                                };
                            })
                            .ToList();

                        var path = Path.Combine(outDir, "screen_workflow_mapping.md");
                        if (rows.Count == 0)
                        {
                            File.WriteAllText(path, "Not found in uploaded files.", Encoding.UTF8);
                            Console.WriteLine($"Wrote: {path}");
                            return 0;
                        }

                        var sb = new StringBuilder();
                        sb.AppendLine("| Screen | Workflow | Trigger | Purpose | ActionsDetected | EvidenceFile | EvidenceSnippet |");
                        sb.AppendLine("|---|---|---|---|---|---|---|");
                        foreach (var r in rows)
                            sb.AppendLine($"| {ProgramLocalHelpers.EscapeMd(r.Screen)} | {ProgramLocalHelpers.EscapeMd(r.Workflow)} | {ProgramLocalHelpers.EscapeMd(r.Trigger)} | {ProgramLocalHelpers.EscapeMd(r.Purpose)} | {ProgramLocalHelpers.EscapeMd(r.Actions)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceFile)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceSnippet)} |");

                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    // RAG mode
                    var smPrompt =
@"Create a Markdown table using ONLY relationships.json and workflows_detailed.json.
Produce a SCREEN -> WORKFLOW mapping for all items where type == ""screen_to_workflow"".
Do NOT invent anything. Join to workflows_detailed.json by workflow name.
If zero screen_to_workflow items, output exactly: Not found in uploaded files.
Columns: Screen | Workflow | Trigger | Purpose | ActionsDetected | EvidenceFile | EvidenceSnippet";

                    var smMd   = await OpenAIHttp.AskWithFileSearch(http, model, vsId, smPrompt);
                    var smPath = Path.Combine(outDir, "screen_workflow_mapping.md");
                    File.WriteAllText(smPath, smMd, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {smPath}");
                    return 0;
                }

                throw new Exception("Unknown generate type. Use: overview | workflows | faq | diagrams | erd | screen-mapping");
            }

            // ──────────────────────────────────────────────────────────────────────
            // export
            // ──────────────────────────────────────────────────────────────────────
            if (cmd == "export")
            {
                if (args.Length < 2) throw new Exception("export requires: word | pdf | excel");
                var kind     = args[1].ToLowerInvariant();
                var overview  = Path.Combine(outDir, "overview.md");
                var workflows = Path.Combine(outDir, "workflows.md");
                var faq       = Path.Combine(outDir, "faq.md");

                if (!File.Exists(overview) || !File.Exists(workflows) || !File.Exists(faq))
                    throw new Exception($"Missing markdown files in {outDir}. Run generate first.");

                if (kind == "word")  { Exporting.ExportWord(outDir, overview, workflows, faq); Console.WriteLine("Wrote Word docs into: " + outDir); return 0; }
                if (kind == "pdf")   { Exporting.ExportPdf(outDir,  overview, workflows, faq); Console.WriteLine("Wrote PDFs into: "      + outDir); return 0; }

                if (kind == "excel")
                {
                    var chunksDir = GetFlag("--chunks");
                    if (string.IsNullOrWhiteSpace(chunksDir))
                        throw new Exception("export excel requires: --chunks \"<chunks_folder>\"");
                    ExcelExport.Export(chunksDir!, outDir);
                    Console.WriteLine("Wrote Excel into: " + outDir);
                    return 0;
                }

                throw new Exception("export requires: word | pdf | excel");
            }

            // ──────────────────────────────────────────────────────────────────────
            // azure-test
            // ──────────────────────────────────────────────────────────────────────
            if (cmd == "azure-test")
            {
                var endpoint   = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
                var key        = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
                var azureModel = GetFlag("--model") ?? "gpt-4.1";

                if (string.IsNullOrWhiteSpace(endpoint)) throw new Exception("Missing env var: AZURE_OPENAI_ENDPOINT");
                if (string.IsNullOrWhiteSpace(key))      throw new Exception("Missing env var: AZURE_OPENAI_API_KEY");

                using var az = new HttpClient { BaseAddress = new Uri(endpoint!) };
                az.DefaultRequestHeaders.Add("api-key", key);

                var payload = new
                {
                    model    = azureModel,
                    messages = new object[] { new { role = "user", content = "What is the capital of France?" } },
                    temperature = 0.0
                };

                var res  = await az.PostAsync("chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                var text = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) throw new Exception(text);

                using var doc = JsonDocument.Parse(text);
                Console.WriteLine(doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString());
                return 0;
            }

            if (cmd == "demo") { Cli.PrintDemo(outDir); return 0; }

            Cli.PrintUsage();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    static bool IsPowerAppsTriggered(string? trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return false;
        return trigger.Contains("PowerApp", StringComparison.OrdinalIgnoreCase);
    }

    static Dictionary<string, string> AssignIds(HashSet<string> names, string prefix) =>
        names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
             .Select((name, idx) => (name, id: $"{prefix}{idx + 1}"))
             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);
}

// ── Tiny env helper ───────────────────────────────────────────────────────────
internal static class Env
{
    public static string MustEnv(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v)) throw new Exception($"Missing env var: {name}");
        return v!;
    }
}

// ── CLI usage / demo ──────────────────────────────────────────────────────────
internal static class Cli
{
    public static void PrintUsage() => Console.WriteLine(@"
Usage:
  dotnet run -- index --chunks ""<path>"" [--name <name>] [--vs <id>]
  dotnet run -- ask ""<question>"" [--vs <id>] [--model <model>]
  dotnet run -- generate overview        [--chunks ""<path>""] [--out <folder>] [--envmap ""<file>""]
  dotnet run -- generate workflows       --chunks ""<path>"" [--out <folder>] [--envmap ""<file>""]
  dotnet run -- generate faq             [--vs <id>] [--out <folder>]
  dotnet run -- generate diagrams        --chunks ""<path>"" [--out <folder>] [--envmap ""<file>""]
  dotnet run -- generate erd             --chunks ""<path>"" [--out <folder>] [--envmap ""<file>""]
  dotnet run -- generate screen-mapping  --chunks ""<path>"" [--out <folder>]
  dotnet run -- export word|pdf|excel    [--chunks ""<path>""] [--out <folder>]
  dotnet run -- azure-test [--model gpt-4.1]
");

    public static void PrintDemo(string outDir) => Console.WriteLine($@"
Demo outputs folder: {outDir}

Recommended run order:
  dotnet run -- generate overview       --chunks ""<chunks>"" [--envmap ""<envmap.json>""]
  dotnet run -- generate workflows      --chunks ""<chunks>"" [--envmap ""<envmap.json>""]
  dotnet run -- generate diagrams       --chunks ""<chunks>"" [--envmap ""<envmap.json>""]
  dotnet run -- generate erd            --chunks ""<chunks>"" [--envmap ""<envmap.json>""]
  dotnet run -- generate screen-mapping --chunks ""<chunks>""
  dotnet run -- export excel            --chunks ""<chunks>""
");
}

// ── Local JSON models ─────────────────────────────────────────────────────────
internal sealed class WorkflowDetail
{
    [JsonPropertyName("workflow")]   public string Workflow { get; set; } = "";
    [JsonPropertyName("file")]       public string File { get; set; } = "";
    [JsonPropertyName("connectors")] public List<string> Connectors { get; set; } = new();
    [JsonPropertyName("env_vars_used")] public List<string> EnvVarsUsed { get; set; } = new();
    [JsonPropertyName("trigger")]    public string? Trigger { get; set; }
    [JsonPropertyName("purpose")]    public string? Purpose { get; set; }
    [JsonPropertyName("actions_detected")] public List<string>? ActionsDetected { get; set; }
}

internal sealed class RelationshipEdge
{
    [JsonPropertyName("from")]     public string From { get; set; } = "";
    [JsonPropertyName("to")]       public string To   { get; set; } = "";
    [JsonPropertyName("type")]     public string Type { get; set; } = "";
    [JsonPropertyName("evidence")] public string? Evidence { get; set; }
}

internal sealed class CanvasAppDetail
{
    [JsonPropertyName("app")]         public string App { get; set; } = "";
    [JsonPropertyName("screens")]     public List<string> Screens { get; set; } = new();
    [JsonPropertyName("connectors")]  public List<string> Connectors { get; set; } = new();
    [JsonPropertyName("files_seen")]  public List<string> FilesSeen { get; set; } = new();
}

// NEW: local model for modeldrivenapps.json chunk
internal sealed class ModelDrivenAppsSection
{
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("items")]  public List<string> Items { get; set; } = new();
}

// ── Local helpers ─────────────────────────────────────────────────────────────
internal static partial class ProgramLocalHelpers
{
    public static string RequireChunksDir(string? chunksDir)
    {
        if (string.IsNullOrWhiteSpace(chunksDir) || !Directory.Exists(chunksDir))
            throw new Exception($"Missing or invalid --chunks directory: {chunksDir}");
        return chunksDir!;
    }

    public static string ReadChunksFile(string chunksDir, string fileName)
    {
        var path = Path.Combine(chunksDir, fileName);
        if (!File.Exists(path)) throw new Exception($"Missing required chunk file: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>Reads modeldrivenapps.json if it exists; returns empty list if absent.</summary>
    public static List<string> TryReadModelDrivenApps(string chunksDir)
    {
        var path = Path.Combine(chunksDir, "modeldrivenapps.json");
        if (!File.Exists(path)) return new List<string>();
        try
        {
            var section = JsonSerializer.Deserialize<ModelDrivenAppsSection>(File.ReadAllText(path));
            return section?.Items ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    public static string StripPrefix(string s, string prefix) =>
        s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? s[prefix.Length..] : s;

    public static string StripAnyKnownPrefix(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        foreach (var p in new[] { "app:", "screen:", "workflow:", "env:", "connector:" })
            s = StripPrefix(s, p);
        return s;
    }

    public static string EscapeMd(string? s)
    {
        if (s == null) return "";
        return s.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|");
    }

    public static string EscapeMermaidLabel(string? s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }

    public static string CleanWorkflowDisplay(string workflowName)
    {
        if (string.IsNullOrWhiteSpace(workflowName)) return workflowName;
        var parts = workflowName.Split('-', 2);
        if (parts.Length == 2 && LooksLikeGuidSuffix(parts[1]))
            return parts[0];
        return workflowName;
    }

    static bool LooksLikeGuidSuffix(string s) =>
        s.Count(c => c == '-') >= 4 && s.Any(char.IsDigit) && s.Any(char.IsLetter);

    /// <summary>
    /// Generic app name display — strips publisher prefix (first segment before _)
    /// and short trailing ID suffix. Works for any publisher, not just wmreply_.
    /// </summary>
    public static string CleanAppDisplay(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return appName;
        var s = appName;

        // Strip publisher prefix: anything before the first underscore up to 10 chars
        var firstUnderscore = s.IndexOf('_');
        if (firstUnderscore > 0 && firstUnderscore <= 10)
            s = s[(firstUnderscore + 1)..];

        // Strip trailing short ID suffix (e.g., _c933c — <= 8 chars after last _)
        var lastUnderscore = s.LastIndexOf('_');
        if (lastUnderscore > 0 && (s.Length - lastUnderscore - 1) <= 8)
            s = s[..lastUnderscore];

        s = s.Replace("_", " ");
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        }
        return string.Join(" ", parts);
    }

    public static Dictionary<string, string> LoadEnvMap(string? envMapPath)
    {
        if (string.IsNullOrWhiteSpace(envMapPath)) return new(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(envMapPath)) throw new Exception($"envmap file not found: {envMapPath}");
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(envMapPath))
                   ?? new Dictionary<string, string>();
        return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
    }

    public static string EnvDisplay(string envVar, Dictionary<string, string> map)
    {
        if (string.IsNullOrWhiteSpace(envVar)) return envVar;
        if (map.TryGetValue(envVar, out var nice) && !string.IsNullOrWhiteSpace(nice))
            return $"{nice} ({envVar})";
        return envVar;
    }

    public static string EnvMapAsBulletText(Dictionary<string, string> map)
    {
        if (map == null || map.Count == 0) return "(none)";
        var sb = new StringBuilder();
        foreach (var kv in map.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"- {kv.Value} ({kv.Key})");
        return sb.ToString();
    }

    /// <summary>
    /// Extracts env var names from the envvars.json chunk by properly parsing the JSON.
    /// This replaces the old hard-coded wmreply_ text scan and works for any solution.
    /// </summary>
    public static List<string> ExtractEnvVarNamesFromEnvVarsJson(string envvarsJson)
    {
        var found = new List<string>();
        if (string.IsNullOrWhiteSpace(envvarsJson)) return found;

        try
        {
            using var doc  = JsonDocument.Parse(envvarsJson);
            var root = doc.RootElement;

            // Structure: { "exists": bool, "items": [{"name": "varname/"}] }
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                    {
                        var name = nameEl.GetString()?.TrimEnd('/').Trim();
                        if (!string.IsNullOrWhiteSpace(name))
                            found.Add(name);
                    }
                }
            }
        }
        catch { /* JSON parse failure — return empty */ }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}