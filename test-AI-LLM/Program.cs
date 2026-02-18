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

            // Default vector store id (override with --vs)
            string defaultVs = "vs_6972909f03948191a19a88a9fd13e234";
            string vsId = defaultVs;

            // Cheap model by default; override with --model
            string model = "gpt-5-mini";

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");

            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            if (args.Length == 0)
            {
                Cli.PrintUsage();
                return 0;
            }

            string? GetFlag(string name)
            {
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == name && i + 1 < args.Length)
                        return args[i + 1];
                return null;
            }

            // global flags
            var vsFlag = GetFlag("--vs");
            if (!string.IsNullOrWhiteSpace(vsFlag)) vsId = vsFlag!;
            var modelFlag = GetFlag("--model");
            if (!string.IsNullOrWhiteSpace(modelFlag)) model = modelFlag!;
            var outFlag = GetFlag("--out");
            if (!string.IsNullOrWhiteSpace(outFlag)) outDir = outFlag!;

            // optional env var friendly-name mapping file
            // Example JSON: { "wmreply_Replybrary_SP_Site": "SharePoint Site URL", ... }
            var envMapFlag = GetFlag("--envmap");

            Directory.CreateDirectory(outDir);

            var cmd = args[0].ToLowerInvariant();

            // -------- index --------
            if (cmd == "index")
            {
                var chunks = GetFlag("--chunks");
                var name = GetFlag("--name") ?? "replybrary_chunks";

                // allow indexing into an existing vector store id
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

                Console.WriteLine("Done. Save this vector store id and reuse it:");
                Console.WriteLine(targetVs);
                return 0;
            }

            // -------- ask --------
            if (cmd == "ask")
            {
                if (args.Length < 2)
                    throw new Exception("ask requires a question: dotnet run -- ask \"...\"");

                var question = string.Join(" ", args.Skip(1));
                var prompt = PromptRouting.BuildRoutedPrompt(question);

                var answer = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                Console.WriteLine(answer);
                return 0;
            }

            // -------- generate --------
            if (cmd == "generate")
            {
                if (args.Length < 2)
                    throw new Exception("generate requires a type: overview | workflows | faq | diagrams | erd | screen-mapping");

                var kind = args[1].ToLowerInvariant();

                var envMap = ProgramLocalHelpers.LoadEnvMap(envMapFlag);
                var envMapText = ProgramLocalHelpers.EnvMapAsBulletText(envMap);

                // ---- overview (RAG) ----
                if (kind == "overview")
                {
                    // LOCAL deterministic mode: pass --chunks
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var canvasDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var envvarsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");
                        var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var appsAll = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new List<CanvasAppDetail>();
                        var realApps = appsAll
                            .Where(a => !string.IsNullOrWhiteSpace(a.App) && a.App.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        // source bucket entry that actually holds screens
                        var srcBucket = appsAll.FirstOrDefault(a => string.Equals(a.App, "CanvasAppsSrc", StringComparison.OrdinalIgnoreCase));
                        var srcScreens = srcBucket?.Screens ?? new List<string>();
                        var srcConnectors = srcBucket?.Connectors ?? new List<string>();

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new List<WorkflowDetail>();
                        var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson) ?? new List<RelationshipEdge>();

                        var envNames = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

                        // Counts
                        var screenCount = srcScreens.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                        var workflowCount = workflows.Count;
                        var realAppCount = realApps.Count;

                        // Edge counts by type
                        var edgeCounts = edges
                            .GroupBy(e => e.Type ?? "", StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                        // Build overview markdown
                        var sb = new StringBuilder();

                        sb.AppendLine("# Solution overview");
                        sb.AppendLine();
                        sb.AppendLine("This solution package contains Power Apps canvas apps and Power Automate workflows that integrate with SharePoint and Microsoft 365 services.");
                        sb.AppendLine();

                        sb.AppendLine("## Key counts");
                        sb.AppendLine($"- Canvas apps: {realAppCount}");
                        sb.AppendLine($"- Workflows (flows): {workflowCount}");
                        sb.AppendLine($"- Environment variables (referenced): {envNames.Count}");
                        sb.AppendLine($"- Screens (total across apps): {screenCount}");
                        sb.AppendLine();

                        sb.AppendLine("Relationship edges by type:");
                        foreach (var kv in edgeCounts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                            sb.AppendLine($"- {kv.Key}: {kv.Value}");
                        sb.AppendLine();

                        sb.AppendLine("## Canvas apps");
                        foreach (var app in realApps.OrderBy(a => a.App, StringComparer.OrdinalIgnoreCase))
                        {
                            var raw = app.App;
                            var nice = ProgramLocalHelpers.CleanCanvasAppDisplay(raw);

                            // IMPORTANT: assign the CanvasAppsSrc screens/connectors to each real app for client-friendly overview
                            sb.AppendLine($"- {nice} ({raw})");
                            sb.AppendLine($"  - Screens: {screenCount}");
                            sb.AppendLine($"  - Connectors referenced by app: {(srcConnectors.Count == 0 ? "Not found in uploaded files" : string.Join(", ", srcConnectors))}");
                            sb.AppendLine();
                        }

                        sb.AppendLine("## Workflows (flows)");
                        foreach (var wf in workflows.OrderBy(w => w.Workflow, StringComparer.OrdinalIgnoreCase))
                        {
                            var nice = ProgramLocalHelpers.CleanWorkflowDisplay(wf.Workflow);
                            var wfLabel = nice == wf.Workflow ? wf.Workflow : $"{nice} ({wf.Workflow})";
                            sb.AppendLine($"- {wfLabel}");
                        }
                        sb.AppendLine();

                        sb.AppendLine("## Environment variables");
                        foreach (var ev in envNames)
                            sb.AppendLine($"- {ProgramLocalHelpers.EnvDisplay(ev, envMap)}");

                        var overviewPath = Path.Combine(outDir, "overview.md");
                        File.WriteAllText(overviewPath, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {overviewPath}");
                        return 0;
                    }

                    var prompt =
@"Generate a clean Markdown solution overview using ONLY the uploaded solution chunks.

You MUST consult:
- overview.json (counts + top level)
- canvasapps.json + canvasapps_detailed.json
- workflows.json + workflows_detailed.json
- envvars.json
- relationships.json (for relationship edge counts/types)

Rules:
- If a field exists in those files, you MUST use it.
- Only say 'Not found in uploaded files.' if the relevant file truly lacks that data.
- Keep headings and bullet points, client-readable, no raw wiring.

Naming rules:
- Workflows: if a workflow ends with a GUID suffix, display as: CleanName (FullName)
- Environment variables: use friendly names if provided below, format: FriendlyName (Key)

Friendly environment variable names:
"
+ envMapText
+ @"

Include:
1) Plain-English overview paragraph
2) Counts: canvas apps, workflows, env vars, screens, relationship edges (by type)
3) Canvas Apps section: app names + screen counts + connectors (from canvasapps_detailed.json)
4) Workflows section: list workflows with trigger + purpose + actions_detected (from workflows_detailed.json), apply naming rules
5) Environment variables list (from envvars.json), apply naming rules
";

                    //  actually write the file, and remove the bad `path` reference
                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);

                    var overviewRagPath = Path.Combine(outDir, "overview.md");
                    File.WriteAllText(overviewRagPath, md, Encoding.UTF8);

                    Console.WriteLine($"Wrote: {overviewRagPath}");
                    return 0;
                }

                // ---- workflows ----
                if (kind == "workflows")
                {
                    // LOCAL deterministic mode: pass --chunks
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson)
                                       ?? new List<WorkflowDetail>();

                        var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)
                                   ?? new List<RelationshipEdge>();

                        var screenToWorkflow = edges
                            .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        var screensByWorkflow = screenToWorkflow
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

                    // RAG mode (unchanged)
                    var prompt =
@"Summarise each workflow in Markdown using ONLY the uploaded chunks.

You MUST consult:
- workflows_detailed.json for: workflow, trigger, connectors, env_vars_used, purpose, actions_detected
- relationships.json to find screens invoking workflows (type == screen_to_workflow)

For each workflow include:
- Workflow name
- Trigger (use workflows_detailed.json trigger)
- Purpose (use workflows_detailed.json purpose)
- Actions detected (use workflows_detailed.json actions_detected, if present)
- Connectors used (use workflows_detailed.json connectors)
- Environment variables referenced (use workflows_detailed.json env_vars_used)
- Invoked from screens: list screens that point to this workflow (from relationships.json)

Rules:
- DO NOT invent actions or purpose not present in workflows_detailed.json.
- If a workflow field is empty in workflows_detailed.json, then and only then write: Not found in uploaded files.
";

                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var aiPath = Path.Combine(outDir, "workflows.md");
                    File.WriteAllText(aiPath, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {aiPath}");
                    return 0;
                }

                // ---- faq (RAG) ----
                if (kind == "faq")
                {
                    var prompt =
@"Create a Markdown FAQ for the solution using ONLY the uploaded chunks.
Include ~10 Q&As (workflows, env vars, canvas apps, what the solution contains).
If info is missing, say 'Not found in uploaded files.' Keep it concise.";

                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var path = Path.Combine(outDir, "faq.md");
                    File.WriteAllText(path, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {path}");
                    return 0;
                }

                // ---- diagrams ----
                if (kind == "diagrams")
                {
                    // LOCAL deterministic mode: pass --chunks
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var canvasDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var envvarsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

                        var apps = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new List<CanvasAppDetail>();
                        apps = apps
                            .Where(a => !string.IsNullOrWhiteSpace(a.App)
                                && a.App.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
                                .ToList();

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new List<WorkflowDetail>();

                        var envNames = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

                        var sb = new StringBuilder();
                        sb.AppendLine("flowchart LR");
                        sb.AppendLine("  subgraph CanvasApps");

                        // IDs CA1..CA{n}
                        for (int i = 0; i < apps.Count; i++)
                        {
                            var raw = apps[i].App;
                            var nice = ProgramLocalHelpers.CleanCanvasAppDisplay(raw);
                            var label = $"{nice} ({raw})";
                            sb.AppendLine($"    CA{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");
                        sb.AppendLine();
                        sb.AppendLine("  subgraph Workflows");

                        // IDs W1..Wn
                        for (int i = 0; i < workflows.Count; i++)
                        {
                            var raw = workflows[i].Workflow;
                            var nice = ProgramLocalHelpers.CleanWorkflowDisplay(raw);
                            var label = nice == raw ? raw : $"{nice} ({raw})";
                            sb.AppendLine($"    W{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");
                        sb.AppendLine();
                        sb.AppendLine("  subgraph EnvironmentVariables");
                        sb.AppendLine("    EVH[\"Environment Variables (shared)\"]");

                        // IDs E1..En
                        for (int i = 0; i < envNames.Count; i++)
                        {
                            var raw = envNames[i];
                            var label = ProgramLocalHelpers.EnvDisplay(raw, envMap);
                            sb.AppendLine($"    E{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");
                        sb.AppendLine();

                        //  only connect to workflows that are PowerApps-triggered
                        sb.AppendLine("  %% Canvas Apps -> Workflows (PowerApps-triggered only)");
                        for (int ca = 0; ca < apps.Count; ca++)
                        {
                            for (int w = 0; w < workflows.Count; w++)
                            {
                                if (!IsPowerAppsTriggered(workflows[w].Trigger))
                                    continue;

                                sb.AppendLine($"  CA{ca + 1} --> W{w + 1}");
                            }
                        }

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

                    // DO NOT redeclare envMapText here (it already exists above)
                    // RAG fallback 
                    var prompt =
@"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.

It MUST include all three groups as explicit nodes:
1) Canvas Apps (both apps as nodes)
2) Workflows (each workflow as its own node — do NOT use a single 'Workflows' hub node)
3) Environment Variables (each env var as its own node)

Naming rules:
- Workflows: if a workflow ends with a GUID suffix, label as: CleanName (FullName)
- Environment variables: use friendly names if provided below, format: FriendlyName (Key)

Friendly environment variable names:
"
+ envMapText
+ @"

Connections rules:
- Connect each Canvas App node to each Workflow node (high-level relationship).
- Connect each Workflow node to a hub node named: Environment Variables (shared)
- Connect that hub node to EVERY environment variable node.
- Do NOT invent per-workflow env var mappings unless explicitly stated in uploaded chunks.

Formatting rules:
- Use subgraphs named exactly: CanvasApps, Workflows, EnvironmentVariables
- Use safe IDs:
  - CA1, CA2 for canvas apps
  - W1..W10 for workflows
  - EVH for env var hub
  - E1..E16 for env vars
- Labels must use the real names from the uploaded chunks (apply naming rules above).
- Output ONLY valid Mermaid code. No second diagram. No markdown fences.";

                    var mermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

                    var ragPath = Path.Combine(outDir, "architecture.mmd");
                    File.WriteAllText(ragPath, mermaid, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {ragPath}");
                    return 0;
                }

                // ---- erd ----
                if (kind == "erd")
                {
                    // LOCAL deterministic mode: pass --chunks
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");
                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var canvasDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
                        var envvarsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

                        var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson) ?? new List<RelationshipEdge>();
                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new List<WorkflowDetail>();
                        var apps = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new List<CanvasAppDetail>();
                        apps = apps
                            .Where(a => !string.IsNullOrWhiteSpace(a.App)
                                && a.App.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        // env names from envvars.json
                        var envNames = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

                        // Build lookup sets from relationships.json (ONLY real edges)
                        bool IsAllowedType(string? t)
                        {
                            if (string.IsNullOrWhiteSpace(t)) return false;
                            return t.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase)
                                || t.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase)
                                || t.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase)
                                || t.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase)
                                || t.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase);
                        }

                        var allowedEdges = edges.Where(e => IsAllowedType(e.Type)).ToList();

                        // Collect nodes used by edges
                        var appNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var screenNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var workflowNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var connectorNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var envNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var e in allowedEdges)
                        {
                            var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
                            var to = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);

                            if (e.Type.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase))
                            {
                                appNamesUsed.Add(from);
                                screenNamesUsed.Add(to);
                            }
                            else if (e.Type.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            {
                                screenNamesUsed.Add(from);
                                workflowNamesUsed.Add(to);
                            }
                            else if (e.Type.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase))
                            {
                                workflowNamesUsed.Add(from);
                                envNamesUsed.Add(to);
                            }
                            else if (e.Type.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase))
                            {
                                workflowNamesUsed.Add(from);
                                connectorNamesUsed.Add(to);
                            }
                            else if (e.Type.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase))
                            {
                                appNamesUsed.Add(from);
                                connectorNamesUsed.Add(to);
                            }
                        }

                        // Helpful: if env vars exist in envvars.json but not referenced by edges, we still keep them out (ERD rules say "ONLY real edges")
                        // BUT for node definitions, we should define only nodes touched by edges to keep diagram clean.

                        // Build workflow lookup for label details
                        var wfByName = workflows.ToDictionary(w => w.Workflow, w => w, StringComparer.OrdinalIgnoreCase);

                        // Apps label lookup (for nicer labels)
                        var appSetFromChunks = new HashSet<string>(apps.Select(a => a.App), StringComparer.OrdinalIgnoreCase);

                        // Assign IDs
                        var appIds = appNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Select((name, idx) => (name, id: $"CA{idx + 1}"))
                            .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

                        var screenIds = screenNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Select((name, idx) => (name, id: $"S{idx + 1}"))
                            .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

                        var wfIds = workflowNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Select((name, idx) => (name, id: $"W{idx + 1}"))
                            .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

                        var envIds = envNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Select((name, idx) => (name, id: $"E{idx + 1}"))
                            .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

                        var connIds = connectorNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Select((name, idx) => (name, id: $"C{idx + 1}"))
                            .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

                        // Mermaid output
                        var sb = new StringBuilder();
                        sb.AppendLine("flowchart LR");

                        // Subgraphs (optional but makes it readable)
                        sb.AppendLine("  subgraph CanvasApps");
                        foreach (var app in appIds.Keys)
                        {
                            var raw = app;
                            // If edges use raw names without publisher prefix, still try to label nicely.
                            var nice = ProgramLocalHelpers.CleanCanvasAppDisplay(raw);
                            // If raw appears to be a real raw-name from chunks, show Nice (Raw). Otherwise show Nice.
                            var label = appSetFromChunks.Contains(raw) ? $"{nice} ({raw})" : nice;
                            sb.AppendLine($"    {appIds[app]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                        }
                        sb.AppendLine("  end");

                        sb.AppendLine("  subgraph Screens");
                        foreach (var scr in screenIds.Keys)
                            sb.AppendLine($"    {screenIds[scr]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(scr)}\"]");
                        sb.AppendLine("  end");

                        sb.AppendLine("  subgraph Workflows");
                        foreach (var wf in wfIds.Keys)
                        {
                            var raw = wf;
                            var nice = ProgramLocalHelpers.CleanWorkflowDisplay(raw);
                            var label = nice == raw ? raw : $"{nice} ({raw})";
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
                                var raw = ev;
                                var label = ProgramLocalHelpers.EnvDisplay(raw, envMap);
                                sb.AppendLine($"    {envIds[ev]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
                            }
                            sb.AppendLine("  end");
                        }

                        sb.AppendLine();

                        // edges (ONLY from relationships.json)
                        foreach (var e in allowedEdges)
                        {
                            var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
                            var to = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);

                            if (e.Type.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase))
                            {
                                if (appIds.TryGetValue(from, out var aId) && screenIds.TryGetValue(to, out var sId))
                                    sb.AppendLine($"  {aId} --> {sId}");
                            }
                            else if (e.Type.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            {
                                if (screenIds.TryGetValue(from, out var sId) && wfIds.TryGetValue(to, out var wId))
                                    sb.AppendLine($"  {sId} --> {wId}");
                            }
                            else if (e.Type.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase))
                            {
                                if (wfIds.TryGetValue(from, out var wId) && envIds.TryGetValue(to, out var eId))
                                    sb.AppendLine($"  {wId} --> {eId}");
                            }
                            else if (e.Type.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase))
                            {
                                if (wfIds.TryGetValue(from, out var wId) && connIds.TryGetValue(to, out var cId))
                                    sb.AppendLine($"  {wId} --> {cId}");
                            }
                            else if (e.Type.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase))
                            {
                                if (appIds.TryGetValue(from, out var aId) && connIds.TryGetValue(to, out var cId))
                                    sb.AppendLine($"  {aId} --> {cId}");
                            }
                        }

                        var path = Path.Combine(outDir, "erd.mmd");
                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    //  DO NOT redeclare envMapText here (it already exists above)
                    // RAG fallback (unchanged)
                    var prompt =
@"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.

You MUST consult:
- relationships.json (edges)
- workflows_detailed.json (workflow labels + purpose)
- canvasapps_detailed.json (app + screens)
- envvars.json (env var names)

Naming rules:
- Workflows: if a workflow ends with a GUID suffix, label as: CleanName (FullName)
- Environment variables: use friendly names if provided below, format: FriendlyName (Key)

Friendly environment variable names:
"
+ envMapText
+ @"

Rules:
- Use ONLY real edges from relationships.json.
- Render:
  - app_to_screen edges
  - screen_to_workflow edges
  - workflow_to_env edges
  - workflow_to_connector edges (if present)
  - app_to_connector edges (if present)
- Do NOT connect everything-to-everything.
- Labels must follow naming rules above.
- Output ONLY valid Mermaid code, no markdown fences.
";

                    var mermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

                    var pathRag = Path.Combine(outDir, "erd.mmd");
                    File.WriteAllText(pathRag, mermaid, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {pathRag}");
                    return 0;
                }

                // ---- screen-mapping ----
                if (kind == "screen-mapping")
                {
                    // LOCAL deterministic mode: pass --chunks
                    var chunksFlag = GetFlag("--chunks");
                    if (!string.IsNullOrWhiteSpace(chunksFlag))
                    {
                        var chunksDir = ProgramLocalHelpers.RequireChunksDir(chunksFlag);

                        var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
                        var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

                        var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson)
                                       ?? new List<WorkflowDetail>();

                        var wfByName = workflows.ToDictionary(w => w.Workflow, w => w, StringComparer.OrdinalIgnoreCase);

                        var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)
                                   ?? new List<RelationshipEdge>();

                        var rows = edges
                            .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
                            .Select(e =>
                            {
                                var screen = ProgramLocalHelpers.StripPrefix(e.From, "screen:");
                                var workflow = ProgramLocalHelpers.StripPrefix(e.To, "workflow:");

                                var workflowDisplay = ProgramLocalHelpers.CleanWorkflowDisplay(workflow);
                                var workflowOut = workflowDisplay == workflow ? workflow : $"{workflowDisplay} ({workflow})";

                                wfByName.TryGetValue(workflow, out var wf);

                                var trigger = wf?.Trigger ?? "";
                                var purpose = wf?.Purpose ?? "";
                                var actions = wf?.ActionsDetected ?? new List<string>();

                                var evidenceFile = "";
                                var evidenceSnippet = "";
                                if (!string.IsNullOrWhiteSpace(e.Evidence))
                                {
                                    var idx = e.Evidence.IndexOf(':');
                                    if (idx >= 0)
                                    {
                                        evidenceFile = e.Evidence.Substring(0, idx).Trim();
                                        evidenceSnippet = e.Evidence.Substring(idx + 1).Trim();
                                    }
                                    else
                                    {
                                        evidenceSnippet = e.Evidence.Trim();
                                    }
                                }

                                if (evidenceSnippet.Length > 120) evidenceSnippet = evidenceSnippet.Substring(0, 120) + "...";

                                return new
                                {
                                    Screen = screen,
                                    Workflow = workflowOut,
                                    Trigger = string.IsNullOrWhiteSpace(trigger) ? "Not found in uploaded files" : trigger,
                                    Purpose = string.IsNullOrWhiteSpace(purpose) ? "Not found in uploaded files" : purpose,
                                    Actions = actions.Count == 0 ? "Not found in uploaded files" : string.Join(", ", actions),
                                    EvidenceFile = string.IsNullOrWhiteSpace(evidenceFile) ? "Not found in uploaded files" : evidenceFile,
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
                        {
                            sb.AppendLine($"| {ProgramLocalHelpers.EscapeMd(r.Screen)} | {ProgramLocalHelpers.EscapeMd(r.Workflow)} | {ProgramLocalHelpers.EscapeMd(r.Trigger)} | {ProgramLocalHelpers.EscapeMd(r.Purpose)} | {ProgramLocalHelpers.EscapeMd(r.Actions)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceFile)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceSnippet)} |");
                        }

                        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        Console.WriteLine($"Wrote: {path}");
                        return 0;
                    }

                    // RAG fallback 
                    var prompt =
@"Create a Markdown table using ONLY:
- relationships.json
- workflows_detailed.json

Goal:
- Produce a SCREEN -> WORKFLOW mapping for ALL items where type == ""screen_to_workflow"".

Rules:
- Do NOT invent anything.
- You MUST join relationships.json to workflows_detailed.json by workflow name.
- Output ONLY the Markdown table (no explanation).
- If there are zero screen_to_workflow items, output exactly: Not found in uploaded files.

Table columns MUST be exactly:
Screen | Workflow | Trigger | Purpose | ActionsDetected | EvidenceFile | EvidenceSnippet

Formatting:
- Screen: use the 'from' field but strip the leading 'screen:' if present (leave the rest)
- Workflow: strip leading 'workflow:' if present
- Trigger/Purpose/ActionsDetected: from workflows_detailed.json (actions join as comma-separated)
- EvidenceFile: extract filename portion from evidence up to first ':' (e.g. 'Client Info Screen.fx.yaml')
- EvidenceSnippet: include the '<FlowName>.Run(' snippet trimmed to ~120 chars, taken from evidence.
";

                    var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
                    var aiPath = Path.Combine(outDir, "screen_workflow_mapping.md");
                    File.WriteAllText(aiPath, md, Encoding.UTF8);
                    Console.WriteLine($"Wrote: {aiPath}");
                    return 0;
                }

                throw new Exception("Unknown generate type. Use: overview | workflows | faq | diagrams | erd | screen-mapping");
            }

            // -------- export --------
            if (cmd == "export")
            {
                if (args.Length < 2)
                    throw new Exception("export requires: word | pdf | excel");

                var kind = args[1].ToLowerInvariant();

                var overview = Path.Combine(outDir, "overview.md");
                var workflows = Path.Combine(outDir, "workflows.md");
                var faq = Path.Combine(outDir, "faq.md");

                if (!File.Exists(overview) || !File.Exists(workflows) || !File.Exists(faq))
                    throw new Exception($"Missing markdown files in {outDir}. Run generate first.");

                if (kind == "word")
                {
                    Exporting.ExportWord(outDir, overview, workflows, faq);
                    Console.WriteLine("Wrote Word docs into: " + outDir);
                    return 0;
                }

                if (kind == "pdf")
                {
                    Exporting.ExportPdf(outDir, overview, workflows, faq);
                    Console.WriteLine("Wrote PDFs into: " + outDir);
                    return 0;
                }

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

            // -------- azure-test --------
            if (cmd == "azure-test")
            {
                var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
                var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new Exception("Missing env var: AZURE_OPENAI_ENDPOINT");
                if (string.IsNullOrWhiteSpace(key))
                    throw new Exception("Missing env var: AZURE_OPENAI_API_KEY");

                var azureModel = GetFlag("--model") ?? "gpt-4.1";

                using var az = new HttpClient { BaseAddress = new Uri(endpoint!) };
                az.DefaultRequestHeaders.Add("api-key", key);

                var payload = new
                {
                    model = azureModel,
                    messages = new object[]
                    {
                        new { role = "user", content = "What is the capital of France?" }
                    },
                    temperature = 0.0
                };

                var res = await az.PostAsync(
                    "chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                );

                var text = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                    throw new Exception(text);

                using var doc = JsonDocument.Parse(text);
                var answer = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                Console.WriteLine(answer);
                return 0;
            }

            // -------- demo --------
            if (cmd == "demo")
            {
                Console.WriteLine("Demo outputs folder:");
                Console.WriteLine(outDir);
                Console.WriteLine();
                Console.WriteLine("Recommended demo run:");
                Console.WriteLine("  dotnet run -- generate overview [--envmap \"<envmap.json>\"]");
                Console.WriteLine("  dotnet run -- generate workflows --chunks \"<chunks>\" [--envmap \"<envmap.json>\"]");
                Console.WriteLine("  dotnet run -- generate faq");
                Console.WriteLine("  dotnet run -- generate diagrams --chunks \"<chunks>\" [--envmap \"<envmap.json>\"]");
                Console.WriteLine("  dotnet run -- generate screen-mapping --chunks \"<chunks>\"");
                Console.WriteLine("  dotnet run -- generate erd --chunks \"<chunks>\" [--envmap \"<envmap.json>\"]");
                Console.WriteLine("  dotnet run -- export word");
                Console.WriteLine("  dotnet run -- export pdf");
                Console.WriteLine("  dotnet run -- export excel --chunks \"<chunks>\"");
                Console.WriteLine();
                Console.WriteLine("Azure test:");
                Console.WriteLine("  dotnet run -- azure-test [--model gpt-4.1]");
                Console.WriteLine();
                Console.WriteLine("Open outputs in Finder:");
                Console.WriteLine($"  open \"{outDir}\"");
                return 0;
            }

            Cli.PrintUsage();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    // helper: used by diagrams local mode to avoid "connect everything to everything"
    static bool IsPowerAppsTriggered(string? trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return false;
        return trigger.Contains("PowerAppV2", StringComparison.OrdinalIgnoreCase);
    }
}

// tiny helpers
internal static class Env
{
    public static string MustEnv(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {name}");
        return v!;
    }
}

internal static class Cli
{
    public static void PrintUsage()
    {
        Console.WriteLine(@"
Usage:

  dotnet run -- index --chunks ""<chunks_folder>"" [--name <vector_store_name>] [--vs <existing_vector_store_id>]

  dotnet run -- ask ""<question>"" [--vs <vector_store_id>] [--model <model>]

  dotnet run -- generate overview        [--out <folder>] [--vs <vector_store_id>] [--model <model>] [--envmap ""<envmap.json>""]
  dotnet run -- generate workflows       [--out <folder>] [--vs <vector_store_id>] [--model <model>] [--chunks ""<chunks_folder>"" --envmap ""<envmap.json>""]
  dotnet run -- generate faq             [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate diagrams        [--out <folder>] [--vs <vector_store_id>] [--model <model>] [--chunks ""<chunks_folder>"" --envmap ""<envmap.json>""]
  dotnet run -- generate erd             [--out <folder>] [--vs <vector_store_id>] [--model <model>] [--chunks ""<chunks_folder>"" --envmap ""<envmap.json>""]
  dotnet run -- generate screen-mapping  [--out <folder>] [--vs <vector_store_id>] [--model <model>] [--chunks ""<chunks_folder>"" --envmap ""<envmap.json>""]

  dotnet run -- export word [--out <folder>]
  dotnet run -- export pdf  [--out <folder>]
  dotnet run -- export excel --chunks ""<chunks_folder>"" [--out <folder>]

  dotnet run -- azure-test [--model gpt-4.1]

Notes:
- index = one-time cost (uploads + embeddings)
- ask / generate (RAG) requires OPENAI_API_KEY + vector store id
- workflows/screen-mapping/diagrams/erd (local deterministic with --chunks) do NOT require OpenAI calls
- --envmap is OPTIONAL and is NOT part of chunks; it is a separate JSON file you pass in.
");
    }
}

// --------------------
// Local JSON models (deterministic generation; avoids RAG retrieval failures)
// --------------------
internal sealed class WorkflowDetail
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
    public string? Trigger { get; set; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("actions_detected")]
    public List<string>? ActionsDetected { get; set; }
}

internal sealed class RelationshipEdge
{
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("evidence")]
    public string? Evidence { get; set; }
}

// Canvas app models for local diagrams/erd
internal sealed class CanvasAppDetail
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

internal static partial class ProgramLocalHelpers
{
    public static string RequireChunksDir(string? chunksDir)
    {
        if (string.IsNullOrWhiteSpace(chunksDir) || !Directory.Exists(chunksDir))
            throw new Exception("Missing or invalid --chunks. Example: --chunks \"/Users/daraling/Downloads/Replybrary_reports/chunks\"");
        return chunksDir!;
    }

    public static string ReadChunksFile(string chunksDir, string fileName)
    {
        var path = Path.Combine(chunksDir, fileName);
        if (!File.Exists(path)) throw new Exception($"Missing required chunk file: {path}");
        return File.ReadAllText(path);
    }

    public static string StripPrefix(string s, string prefix)
    {
        if (s == null) return "";
        return s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? s.Substring(prefix.Length) : s;
    }

    //  strip any common prefixes in relationships.json
    public static string StripAnyKnownPrefix(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var prefixes = new[] { "app:", "screen:", "workflow:", "env:", "connector:" };
        foreach (var p in prefixes)
            s = StripPrefix(s, p);
        return s;
    }

    public static string EscapeMd(string s)
    {
        if (s == null) return "";
        return s.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|");
    }

    //  safe escaping for Mermaid labels (quoted strings)
    public static string EscapeMermaidLabel(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }

    // -------- helpers for clean workflow names + env var friendly names --------

    public static string CleanWorkflowDisplay(string workflowName)
    {
        if (string.IsNullOrWhiteSpace(workflowName)) return workflowName;

        var parts = workflowName.Split('-', 2);
        if (parts.Length == 2 && LooksLikeGuidSuffix(parts[1]))
            return parts[0];

        return workflowName;
    }

    static bool LooksLikeGuidSuffix(string s)
    {
        return s.Count(c => c == '-') >= 4 && s.Any(char.IsDigit) && s.Any(char.IsLetter);
    }

    //  canvas app name cleaning for Mermaid labels
    public static string CleanCanvasAppDisplay(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName)) return appName;

        // Example: wmreply_replybraryv2_c933c -> Replybraryv2 / Replybraryv2 (depending on source)
        var s = appName;

        if (s.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("wmreply_".Length);

        // remove trailing _xxxxx (<=8 chars) if present
        var lastUnderscore = s.LastIndexOf('_');
        if (lastUnderscore > 0 && (s.Length - lastUnderscore) <= 8)
            s = s.Substring(0, lastUnderscore);

        s = s.Replace("_", " ");

        // Title-case-ish per word
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 0) continue;
            parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
        }
        return string.Join(" ", parts);
    }

    public static Dictionary<string, string> LoadEnvMap(string? envMapPath)
    {
        if (string.IsNullOrWhiteSpace(envMapPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(envMapPath))
            throw new Exception($"envmap file not found: {envMapPath}");

        var json = File.ReadAllText(envMapPath);

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
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

    //env var extraction for local diagrams/erd (envvars.json isn’t a plain string array)
    public static List<string> ExtractEnvVarNamesFromEnvVarsJson(string envvarsJson)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(envvarsJson)) return found.ToList();

        var token = "wmreply_";
        int i = 0;
        while (i < envvarsJson.Length)
        {
            var idx = envvarsJson.IndexOf(token, i, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            int end = idx;
            while (end < envvarsJson.Length)
            {
                char c = envvarsJson[end];
                if (char.IsLetterOrDigit(c) || c == '_') end++;
                else break;
            }

            var name = envvarsJson.Substring(idx, end - idx).Trim();
            if (!string.IsNullOrWhiteSpace(name))
                found.Add(name);

            i = end;
        }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
