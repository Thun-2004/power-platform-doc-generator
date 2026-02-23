
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text.Json.Serialization;
// using System.Threading.Tasks;

// using System.Diagnostics;
// using System.Net.Http.Headers;
// using Microsoft.AspNetCore.Http;
// using System.Text;
// using System.Text.Json;
// using System.IO.Compression;
// using backend.Domain; 

// using backend.Application.Helpers; 
// using backend.Application.Parser;
// using backend.Application.Interfaces;
// using System.Reflection.PortableExecutable;
// using System.Security.Cryptography.X509Certificates;
// using System.ComponentModel;
// using System.Data.SqlTypes;


// namespace backend.Application.LLM;

// //TODO: env.json upload , env var -> null ? 
// //word or pdf mode 
// public class FileProcessing
// {
//     private readonly IJobStore _jobs;

//     public FileProcessing(IJobStore jobs)
//     {
//         _jobs = jobs;
//     }

//     private async Task UploadChunksToVectorStoreAsync(
//         HttpClient http,
//         string vsId,
//         string chunksDir,
//         int maxConcurrency = 5)
//     {
//         var files = Directory.GetFiles(chunksDir, "*.json", SearchOption.AllDirectories);

//         using var semaphore = new System.Threading.SemaphoreSlim(maxConcurrency);
//         var tasks = new List<Task>();

//         foreach (var f in files)
//         {
//             await semaphore.WaitAsync();

//             tasks.Add(Task.Run(async () =>
//             {
//                 try
//                 {
//                     Console.WriteLine($"Uploading: {Path.GetFileName(f)}");
//                     var fileId = await OpenAIHttp.UploadFile(http, f);
//                     await OpenAIHttp.AttachFileToVectorStore(http, vsId, fileId);
//                     Console.WriteLine($"  attached file_id={fileId}");
//                 }
//                 finally
//                 {
//                     semaphore.Release();
//                 }
//             }));
//         }

//         await Task.WhenAll(tasks);
//     }

//     public string CreateFile(string originalName, string ext, string targetDir)
//     {
//         var path = Path.Combine(targetDir, originalName);
//         var name = Path.GetFileNameWithoutExtension(originalName);
//         var count = 1;
//         while (System.IO.File.Exists(path))
//         {
//             var newName = $"{name}({count}){ext}";
//             path = Path.Combine(targetDir, newName);
//             count++;
//         }
//         return path; 
//     }

//     public async Task<string> ProcessFile(List<string> SelectedOutputTypes, string jobId)
//     {
//         //unzip file
//         var inputPath = _jobs.Get(jobId).ZipFilePath;
//         var extractPath = Path.Combine(Path.GetTempPath(), "extract", Guid.NewGuid().ToString());
//         Directory.CreateDirectory(extractPath);
//         ZipFile.ExtractToDirectory(inputPath, extractPath);

//         //upload file to vector db
//         var extractedJsonFiles = Directory.GetFiles(extractPath, "*.json", SearchOption.AllDirectories);
//         if (extractedJsonFiles.Length == 0)
//             throw new Exception("No .json files found after unzip. Is this the parsed output zip?");
        
//         //create dir for each job id
//         string jobDir = Path.GetFullPath(
//             Path.Combine(
//                 "..", "backend.Infrastructure", "FileStorages", "ParsedOutputs", $"{jobId}"
//             )
//         ); 
//         if (!Directory.Exists(jobDir))
//         {
//             Directory.CreateDirectory(jobDir);
//         }

//         string chunksDir = SolutionParser.Run(extractPath, jobDir);

//         var response = "Success";
//         List<string> output_files = new List<string>();

//         try
//         {
//             //var apiKey = EnvReader.MustEnv("OPENAI_API_KEY"); 
//             //var apiKey = EnvReader.Load("OPENAI_API_KEY");
//             var apiKey = "REMOVED_OPENAI_KEY";
//             string defaultVs = "vs_6976904735208191b309858e3f2e0f74";

//             string model = "gpt-5-mini";

//             string? wordPath = null; 

//             //create dir for each job id
//             string outDir = Path.GetFullPath(
//                 Path.Combine(
//                     "..",  "backend.Infrastructure", "FileStorages", "RAGOutputs", $"{jobId}"
//                 )
//             );

//             if (!Directory.Exists(outDir))
//             {
//                 Directory.CreateDirectory(outDir);
//             }

//             using var http = new HttpClient
//             {
//                 Timeout = TimeSpan.FromMinutes(10)
//             };
//             http.DefaultRequestHeaders.Authorization =
//                 new AuthenticationHeaderValue("Bearer", apiKey);

//             // start
//             // global flags
//             // var vsFlag = GetFlag("--vs");
//             // if (!string.IsNullOrWhiteSpace(vsFlag)) vsId = vsFlag!;
//             // var modelFlag = GetFlag("--model");
//             // if (!string.IsNullOrWhiteSpace(modelFlag)) model = modelFlag!;
//             // var outFlag = GetFlag("--out");
//             // if (!string.IsNullOrWhiteSpace(outFlag)) outDir = outFlag!;

//             // // optional env var friendly-name mapping file
//             // // Example JSON: { "wmreply_Replybrary_SP_Site": "SharePoint Site URL", ... }
//             // var envMapFlag = GetFlag("--envmap");

//             //vector store setup
//             var name = "replybrary_chunks";
//             var chunks = chunksDir;

//             var vsId = await OpenAIHttp.CreateVectorStore(http, name);

//             Console.WriteLine($"Vector store: {vsId}");

//             await UploadChunksToVectorStoreAsync(http, vsId, chunks!);

//             //new
//             var generationTasks = new List<Task>();

//             for (int type = 0; type < SelectedOutputTypes.Count; type++)
//             {
//                 var rawCmd = SelectedOutputTypes[type];
//                 var cmdLower = rawCmd.ToLowerInvariant().Trim();

//                 // special case: ask (kept sequential like before)
//                 if (cmdLower == "ask")
//                 {
//                     string? question = null;
//                     if (rawCmd.Length > 3)
//                     {
//                         var sepIndex = rawCmd.IndexOfAny(new[] { ':', '=', '|', '?' });
//                         if (sepIndex >= 0 && sepIndex + 1 < rawCmd.Length)
//                             question = rawCmd[(sepIndex + 1)..].Trim();
//                         else
//                             question = rawCmd[3..].Trim();
//                     }
//                     else if (type + 1 < SelectedOutputTypes.Count)
//                     {
//                         question = SelectedOutputTypes[type + 1].Trim();
//                         type++;
//                     }

//                     var prompt = PromptRouting.BuildRoutedPrompt(question);
//                     var answer = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     Console.WriteLine(answer);

//                     _jobs.setOutputFile(jobId, cmdLower, " ");
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmdLower, JobState.Completed);

//                     // same behavior as before: return immediately for ask
//                     return response;
//                 }

//                 // all non-"ask" outputs run in parallel
//                 generationTasks.Add(
//                     GenerateSingleOutputAsync(http, model, vsId, jobId, rawCmd, chunksDir, outDir));
//             }

//             // wait for all selected outputs to finish
//             // await Task.WhenAll(generationTasks);

//             for (int type = 0; type < SelectedOutputTypes.Count; type++)
//             {

//                 var rawCmd = SelectedOutputTypes[type];
//                 var cmd = rawCmd.ToLowerInvariant();

//                 //update job status to processing
//                 _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Processing);

//                 Console.WriteLine($"raw='{SelectedOutputTypes}' len={SelectedOutputTypes.Count}");
//                 Console.WriteLine($"cmd='{cmd}' len={cmd.Length}");
//                 Console.WriteLine("cmd chars: " + string.Join(",", cmd.Select(c => (int)c)));
//                 Console.WriteLine("output type: " + cmd);

//                 // await WaitForVectorStoreFilesReady(http, vsId);

//                 // -------- ask --------
//                 if (cmd == "ask")
//                 {

//                     //TODO: add param that pass in question
//                     string? question = null;
//                     if (rawCmd.Length > 3)
//                     {
//                         var sepIndex = rawCmd.IndexOfAny(new[] { ':', '=', '|', '?' });
//                         if (sepIndex >= 0 && sepIndex + 1 < rawCmd.Length)
//                             question = rawCmd[(sepIndex + 1)..].Trim();
//                         else
//                             question = rawCmd[3..].Trim();
//                     }
//                     else if (type + 1 < SelectedOutputTypes.Count)
//                     {
//                         question = SelectedOutputTypes[type + 1].Trim();
//                         type++;
//                     }
//                     var prompt = PromptRouting.BuildRoutedPrompt(question);

//                     var answer = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     Console.WriteLine(answer);

//                     //TODO: add output file location 
//                     _jobs.setOutputFile(jobId, cmd, " ");

//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);

//                     return response;
//                 }

//                 // -------- generate --------
//                 else if (cmd == "overview")
//                 {
//                     var prompt =
//     @"Generate a clean Markdown solution overview based ONLY on uploaded chunks.
//     Include:
//     - Counts: canvas apps, workflows, env vars, relationship edges (by type), screens (if present)
//     - Connectors used (unique list)
//     - Workflows list
//     - Env var names list
//     - If screens exist: list screens per app (names)
//     Keep it concise with headings + bullet points.";

//                     var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     var path = Path.Combine(outDir, "overview.md");
//                     File.WriteAllText(path, md, Encoding.UTF8);
//                     Console.WriteLine($"Wrote: {path}");

//                     // export to Word
//                     wordPath = Path.Combine(outDir, "Replybrary_Overview.docx");
//                     Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_Overview.docx\" --toc", outDir);

//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                     // return wordPath;
//                 }

//                 else if (cmd == "workflows")
//                 {

//                     if (!Directory.Exists(chunksDir))
//                     {
//                         var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
//                         var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

//                         var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson)
//                                        ?? new List<WorkflowDetail>();

//                         var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)
//                                    ?? new List<RelationshipEdge>();

//                         var screenToWorkflow = edges
//                             .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
//                             .ToList();

//                         var screensByWorkflow = screenToWorkflow
//                             .GroupBy(e => ProgramLocalHelpers.StripPrefix(e.To, "workflow:"))
//                             .ToDictionary(
//                                 g => g.Key,
//                                 g => g.Select(e => ProgramLocalHelpers.StripPrefix(e.From, "screen:"))
//                                       .Distinct(StringComparer.OrdinalIgnoreCase)
//                                       .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                                       .ToList(),
//                                 StringComparer.OrdinalIgnoreCase
//                             );

//                         var sb = new StringBuilder();
//                         sb.AppendLine("# Workflows");
//                         sb.AppendLine();

//                         foreach (var wf in workflows.OrderBy(w => w.Workflow, StringComparer.OrdinalIgnoreCase))
//                         {
//                             var wfDisplay = ProgramLocalHelpers.CleanWorkflowDisplay(wf.Workflow);
//                             sb.AppendLine($"## {wfDisplay} ({wf.Workflow})");
//                             sb.AppendLine();

//                             sb.AppendLine($"- Trigger: {(string.IsNullOrWhiteSpace(wf.Trigger) ? "Not found in uploaded files" : wf.Trigger)}");
//                             sb.AppendLine($"- Purpose: {(string.IsNullOrWhiteSpace(wf.Purpose) ? "Not found in uploaded files" : wf.Purpose)}");

//                             var actions = wf.ActionsDetected ?? new List<string>();
//                             sb.AppendLine($"- Actions detected: {(actions.Count == 0 ? "Not found in uploaded files" : string.Join(", ", actions))}");

//                             sb.AppendLine($"- Connectors: {(wf.Connectors.Count == 0 ? "Not found in uploaded files" : string.Join(", ", wf.Connectors))}");
                            
//                             // TODO: env
//                             // var envList = (wf.EnvVarsUsed ?? new List<string>())
//                             //     .Select(v => ProgramLocalHelpers.EnvDisplay(v, envMap))
//                             //     .ToList();

//                             // sb.AppendLine($"- Environment variables: {(envList.Count == 0 ? "Not found in uploaded files" : string.Join(", ", envList))}");

//                             if (screensByWorkflow.TryGetValue(wf.Workflow, out var screens) && screens.Count > 0)
//                                 sb.AppendLine($"- Invoked from screens: {string.Join(", ", screens)}");
//                             else
//                                 sb.AppendLine("- Invoked from screens: Not found in uploaded files");

//                             sb.AppendLine();
//                         }

//                         var path = Path.Combine(outDir, "workflows.md");
//                         File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
//                         Console.WriteLine($"Wrote: {path}");

//                         wordPath = Path.Combine(outDir, "Replybrary_Workflows.docx");
//                         Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_Workflows.docx\" --toc", outDir);
//                         Console.WriteLine($"Wrote: {wordPath}");

//                         _jobs.setOutputFile(jobId, cmd, wordPath);
//                         //update job status to processing
//                         _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                     }


//                     var prompt =
//                         @"Summarise each workflow in Markdown using ONLY the uploaded chunks.

// You MUST consult:
// - workflows_detailed.json for: workflow, trigger, connectors, env_vars_used, purpose, actions_detected
// - relationships.json to find screens invoking workflows (type == screen_to_workflow)

// For each workflow include:
// - Workflow name
// - Trigger (use workflows_detailed.json trigger)
// - Purpose (use workflows_detailed.json purpose)
// - Actions detected (use workflows_detailed.json actions_detected, if present)
// - Connectors used (use workflows_detailed.json connectors)
// - Environment variables referenced (use workflows_detailed.json env_vars_used)
// - Invoked from screens: list screens that point to this workflow (from relationships.json)

// Rules:
// - DO NOT invent actions or purpose not present in workflows_detailed.json.
// - If a workflow field is empty in workflows_detailed.json, then and only then write: Not found in uploaded files.
// ";

//                     Console.WriteLine("Generating workflows.md...");

//                     var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     var aiPath = Path.Combine(outDir, "workflows.md");
//                     File.WriteAllText(aiPath, md, Encoding.UTF8);
//                     Console.WriteLine($"Wrote: {aiPath}");

//                     wordPath = Path.Combine(outDir, "Replybrary_Workflows.docx");
//                     Exporting.RunProcess("pandoc", $"\"{aiPath}\" -o \"Replybrary_Workflows.docx\" --toc", outDir);
//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                 }

//                 else if (cmd == "faq")
//                 {
//                     var prompt =
//                         @"Create a Markdown FAQ for the solution using ONLY the uploaded chunks.
//                         Include ~10 Q&As (workflows, env vars, canvas apps, what the solution contains).
//                         If info is missing, say 'Not found in uploaded files.' Keep it concise.";

//                     var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     var path = Path.Combine(outDir, "faq.md");
//                     Console.WriteLine($"Wrote: {path}");

//                     File.WriteAllText(path, md, Encoding.UTF8);

//                     wordPath = Path.Combine(outDir, "Replybrary_FAQ.docx");
//                     Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_FAQ.docx\" --toc", outDir);
//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);

//                     // return wordPath;
//                 }


//                 else if (cmd == "diagrams")
//                 {

//                     if (!Directory.Exists(chunksDir))
//                     {
//                         var canvasDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
//                         var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
//                         var envvarsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

//                         var apps = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new List<CanvasAppDetail>();
//                         apps = apps
//                             .Where(a => !string.IsNullOrWhiteSpace(a.App)
//                                 && a.App.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
//                                 .ToList();

//                         var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new List<WorkflowDetail>();

//                         var envNames = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

//                         var sb = new StringBuilder();
//                         sb.AppendLine("flowchart LR");
//                         sb.AppendLine("  subgraph CanvasApps");

//                         // IDs CA1..CA{n}
//                         for (int i = 0; i < apps.Count; i++)
//                         {
//                             var raw = apps[i].App;
//                             var nice = ProgramLocalHelpers.CleanCanvasAppDisplay(raw);
//                             var label = $"{nice} ({raw})";
//                             sb.AppendLine($"    CA{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                         }
//                         sb.AppendLine("  end");
//                         sb.AppendLine();
//                         sb.AppendLine("  subgraph Workflows");

//                         // IDs W1..Wn
//                         for (int i = 0; i < workflows.Count; i++)
//                         {
//                             var raw = workflows[i].Workflow;
//                             var nice = ProgramLocalHelpers.CleanWorkflowDisplay(raw);
//                             var label = nice == raw ? raw : $"{nice} ({raw})";
//                             sb.AppendLine($"    W{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                         }
//                         sb.AppendLine("  end");
//                         sb.AppendLine();
//                         sb.AppendLine("  subgraph EnvironmentVariables");
//                         sb.AppendLine("    EVH[\"Environment Variables (shared)\"]");

//                         // IDs E1..En
//                         for (int i = 0; i < envNames.Count; i++)
//                         {
//                             var raw = envNames[i];
//                             //TODO: env
//                             // var label = ProgramLocalHelpers.EnvDisplay(raw, envMap);
//                             // sb.AppendLine($"    E{i + 1}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                         }
//                         sb.AppendLine("  end");
//                         sb.AppendLine();

//                         //  only connect to workflows that are PowerApps-triggered
//                         sb.AppendLine("  %% Canvas Apps -> Workflows (PowerApps-triggered only)");
//                         for (int ca = 0; ca < apps.Count; ca++)
//                         {
//                             for (int w = 0; w < workflows.Count; w++)
//                             {
//                                 if (!IsPowerAppsTriggered(workflows[w].Trigger))
//                                     continue;

//                                 sb.AppendLine($"  CA{ca + 1} --> W{w + 1}");
//                             }
//                         }

//                         sb.AppendLine();
//                         sb.AppendLine("  %% Workflows -> Env Hub");
//                         for (int w = 0; w < workflows.Count; w++)
//                             sb.AppendLine($"  W{w + 1} --> EVH");

//                         sb.AppendLine();
//                         sb.AppendLine("  %% Env Hub -> each env var");
//                         for (int e = 0; e < envNames.Count; e++)
//                             sb.AppendLine($"  EVH --> E{e + 1}");

//                         var path = Path.Combine(outDir, "architecture.mmd");
//                         File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
//                         Console.WriteLine($"Wrote: {path}");

//                         //TODO: not sure if its word docx export
//                         wordPath = Path.Combine(outDir, "Replybrary_Diagrams.docx");
//                         Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_Diagrams.docx\" --toc", outDir);
//                         Console.WriteLine($"Wrote: {wordPath}");

//                         _jobs.setOutputFile(jobId, cmd, wordPath);
//                         //update job status to processing
//                         _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);

//                     }


//                     var prompt =
//                     @"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.

//                     It MUST include all three groups as explicit nodes:
//                     1) Canvas Apps (both apps as nodes)
//                     2) Workflows (each workflow as its own node — do NOT use a single 'Workflows' hub node)
//                     3) Environment Variables (each env var as its own node)

//                     Naming rules:
//                     - Workflows: if a workflow ends with a GUID suffix, label as: CleanName (FullName)
//                     - Environment variables: use friendly names if provided below, format: FriendlyName (Key)

//                     Friendly environment variable names:
//                     "
//                     + @"

//                     Connections rules:
//                     - Connect each Canvas App node to each Workflow node (high-level relationship).
//                     - Connect each Workflow node to a hub node named: Environment Variables (shared)
//                     - Connect that hub node to EVERY environment variable node.
//                     - Do NOT invent per-workflow env var mappings unless explicitly stated in uploaded chunks.

//                     Formatting rules:
//                     - Use subgraphs named exactly: CanvasApps, Workflows, EnvironmentVariables
//                     - Use safe IDs:
//                     - CA1, CA2 for canvas apps
//                     - W1..W10 for workflows
//                     - EVH for env var hub
//                     - E1..E16 for env vars
//                     - Labels must use the real names from the uploaded chunks (apply naming rules above).
//                     - Output ONLY valid Mermaid code. No second diagram. No markdown fences.";

//                     var mermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

//                     var aiPath = Path.Combine(outDir, "architecture.mmd");
//                     File.WriteAllText(aiPath, mermaid, Encoding.UTF8);
//                     Console.WriteLine($"Wrote: {aiPath}");

//                     //TODO: not sure if its word docx export
//                     wordPath = Path.Combine(outDir, "Replybrary_Diagrams.docx");
//                     Exporting.RunProcess("pandoc", $"\"{aiPath}\" -o \"Replybrary_Diagrams.docx\" --toc", outDir);
//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                 }
//                 else if (cmd == "erd")
//                 {
//                     if (!Directory.Exists(chunksDir))
//                     {
//                         var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");
//                         var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
//                         var canvasDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "canvasapps_detailed.json");
//                         var envvarsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "envvars.json");

//                         var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson) ?? new List<RelationshipEdge>();
//                         var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson) ?? new List<WorkflowDetail>();
//                         var apps = JsonSerializer.Deserialize<List<CanvasAppDetail>>(canvasDetailedJson) ?? new List<CanvasAppDetail>();
//                         apps = apps
//                             .Where(a => !string.IsNullOrWhiteSpace(a.App)
//                                 && a.App.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
//                             .ToList();

//                         // env names from envvars.json
//                         var envNames = ProgramLocalHelpers.ExtractEnvVarNamesFromEnvVarsJson(envvarsJson);

//                         // Build lookup sets from relationships.json (ONLY real edges)
//                         bool IsAllowedType(string? t)
//                         {
//                             if (string.IsNullOrWhiteSpace(t)) return false;
//                             return t.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase)
//                                 || t.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase)
//                                 || t.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase)
//                                 || t.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase)
//                                 || t.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase);
//                         }

//                         var allowedEdges = edges.Where(e => IsAllowedType(e.Type)).ToList();

//                         // Collect nodes used by edges
//                         var appNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                         var screenNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                         var workflowNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                         var connectorNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                         var envNamesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//                         foreach (var e in allowedEdges)
//                         {
//                             var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
//                             var to = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);

//                             if (e.Type.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 appNamesUsed.Add(from);
//                                 screenNamesUsed.Add(to);
//                             }
//                             else if (e.Type.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 screenNamesUsed.Add(from);
//                                 workflowNamesUsed.Add(to);
//                             }
//                             else if (e.Type.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 workflowNamesUsed.Add(from);
//                                 envNamesUsed.Add(to);
//                             }
//                             else if (e.Type.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 workflowNamesUsed.Add(from);
//                                 connectorNamesUsed.Add(to);
//                             }
//                             else if (e.Type.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 appNamesUsed.Add(from);
//                                 connectorNamesUsed.Add(to);
//                             }
//                         }

//                         // Helpful: if env vars exist in envvars.json but not referenced by edges, we still keep them out (ERD rules say "ONLY real edges")
//                         // BUT for node definitions, we should define only nodes touched by edges to keep diagram clean.

//                         // Build workflow lookup for label details
//                         var wfByName = workflows.ToDictionary(w => w.Workflow, w => w, StringComparer.OrdinalIgnoreCase);

//                         // Apps label lookup (for nicer labels)
//                         var appSetFromChunks = new HashSet<string>(apps.Select(a => a.App), StringComparer.OrdinalIgnoreCase);

//                         // Assign IDs
//                         var appIds = appNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                             .Select((name, idx) => (name, id: $"CA{idx + 1}"))
//                             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

//                         var screenIds = screenNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                             .Select((name, idx) => (name, id: $"S{idx + 1}"))
//                             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

//                         var wfIds = workflowNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                             .Select((name, idx) => (name, id: $"W{idx + 1}"))
//                             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

//                         var envIds = envNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                             .Select((name, idx) => (name, id: $"E{idx + 1}"))
//                             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

//                         var connIds = connectorNamesUsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
//                             .Select((name, idx) => (name, id: $"C{idx + 1}"))
//                             .ToDictionary(x => x.name, x => x.id, StringComparer.OrdinalIgnoreCase);

//                         // Mermaid output
//                         var sb = new StringBuilder();
//                         sb.AppendLine("flowchart LR");

//                         // Subgraphs (optional but makes it readable)
//                         sb.AppendLine("  subgraph CanvasApps");
//                         foreach (var app in appIds.Keys)
//                         {
//                             var raw = app;
//                             // If edges use raw names without publisher prefix, still try to label nicely.
//                             var nice = ProgramLocalHelpers.CleanCanvasAppDisplay(raw);
//                             // If raw appears to be a real raw-name from chunks, show Nice (Raw). Otherwise show Nice.
//                             var label = appSetFromChunks.Contains(raw) ? $"{nice} ({raw})" : nice;
//                             sb.AppendLine($"    {appIds[app]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                         }
//                         sb.AppendLine("  end");

//                         sb.AppendLine("  subgraph Screens");
//                         foreach (var scr in screenIds.Keys)
//                             sb.AppendLine($"    {screenIds[scr]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(scr)}\"]");
//                         sb.AppendLine("  end");

//                         sb.AppendLine("  subgraph Workflows");
//                         foreach (var wf in wfIds.Keys)
//                         {
//                             var raw = wf;
//                             var nice = ProgramLocalHelpers.CleanWorkflowDisplay(raw);
//                             var label = nice == raw ? raw : $"{nice} ({raw})";
//                             sb.AppendLine($"    {wfIds[wf]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                         }
//                         sb.AppendLine("  end");

//                         if (connIds.Count > 0)
//                         {
//                             sb.AppendLine("  subgraph Connectors");
//                             foreach (var c in connIds.Keys)
//                                 sb.AppendLine($"    {connIds[c]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(c)}\"]");
//                             sb.AppendLine("  end");
//                         }

//                         if (envIds.Count > 0)
//                         {
//                             sb.AppendLine("  subgraph EnvironmentVariables");
//                             foreach (var ev in envIds.Keys)
//                             {
//                                 var raw = ev;
//                                 //TODO: envMap
//                                 // var label = ProgramLocalHelpers.EnvDisplay(raw, envMap);
//                                 // sb.AppendLine($"    {envIds[ev]}[\"{ProgramLocalHelpers.EscapeMermaidLabel(label)}\"]");
//                             }
//                             sb.AppendLine("  end");
//                         }

//                         sb.AppendLine();

//                         // edges (ONLY from relationships.json)
//                         foreach (var e in allowedEdges)
//                         {
//                             var from = ProgramLocalHelpers.StripAnyKnownPrefix(e.From);
//                             var to = ProgramLocalHelpers.StripAnyKnownPrefix(e.To);

//                             if (e.Type.Equals("app_to_screen", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 if (appIds.TryGetValue(from, out var aId) && screenIds.TryGetValue(to, out var sId))
//                                     sb.AppendLine($"  {aId} --> {sId}");
//                             }
//                             else if (e.Type.Equals("screen_to_workflow", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 if (screenIds.TryGetValue(from, out var sId) && wfIds.TryGetValue(to, out var wId))
//                                     sb.AppendLine($"  {sId} --> {wId}");
//                             }
//                             else if (e.Type.Equals("workflow_to_env", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 if (wfIds.TryGetValue(from, out var wId) && envIds.TryGetValue(to, out var eId))
//                                     sb.AppendLine($"  {wId} --> {eId}");
//                             }
//                             else if (e.Type.Equals("workflow_to_connector", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 if (wfIds.TryGetValue(from, out var wId) && connIds.TryGetValue(to, out var cId))
//                                     sb.AppendLine($"  {wId} --> {cId}");
//                             }
//                             else if (e.Type.Equals("app_to_connector", StringComparison.OrdinalIgnoreCase))
//                             {
//                                 if (appIds.TryGetValue(from, out var aId) && connIds.TryGetValue(to, out var cId))
//                                     sb.AppendLine($"  {aId} --> {cId}");
//                             }
//                         }

//                         var path = Path.Combine(outDir, "erd.mmd");
//                         File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
//                         Console.WriteLine($"Wrote: {path}");

//                         //TODO: not sure if its word docx export
//                         wordPath = Path.Combine(outDir, "Replybrary_Diagrams.docx");
//                         Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_Diagrams.docx\" --toc", outDir);
//                         Console.WriteLine($"Wrote: {wordPath}");

//                         _jobs.setOutputFile(jobId, cmd, wordPath);
//                         //update job status to processing
//                         _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);

//                     }
//                     var prompt =
//                         @"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.

//                         You MUST consult:
//                         - relationships.json (edges)
//                         - workflows_detailed.json (workflow labels + purpose)
//                         - canvasapps_detailed.json (app + screens)
//                         - envvars.json (env var names)

//                         Naming rules:
//                         - Workflows: if a workflow ends with a GUID suffix, label as: CleanName (FullName)
//                         - Environment variables: use friendly names if provided below, format: FriendlyName (Key)

//                         Friendly environment variable names:
//                         "
                        
//                         + @"

//                         Rules:
//                         - Use ONLY real edges from relationships.json.
//                         - Render:
//                         - app_to_screen edges
//                         - screen_to_workflow edges
//                         - workflow_to_env edges
//                         - workflow_to_connector edges (if present)
//                         - app_to_connector edges (if present)
//                         - Do NOT connect everything-to-everything.
//                         - Labels must follow naming rules above.
//                         - Output ONLY valid Mermaid code, no markdown fences.
//                         ";

//                     var mermaid = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

//                     var aiPath = Path.Combine(outDir, "erd.mmd");
//                     File.WriteAllText(aiPath, mermaid, Encoding.UTF8);
//                     Console.WriteLine($"Wrote: {aiPath}");

//                     //TODO: not sure if its word docx export
//                     wordPath = Path.Combine(outDir, "Replybrary_Diagrams.docx");
//                     Exporting.RunProcess("pandoc", $"\"{aiPath}\" -o \"Replybrary_Diagrams.docx\" --toc", outDir);
//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);

//                 }
//                 else if (cmd == "screen-mapping")
//                 {
//                     if (!Directory.Exists(chunksDir))
//                     {
//                         var workflowsDetailedJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "workflows_detailed.json");
//                         var relationshipsJson = ProgramLocalHelpers.ReadChunksFile(chunksDir, "relationships.json");

//                         var workflows = JsonSerializer.Deserialize<List<WorkflowDetail>>(workflowsDetailedJson)
//                                        ?? new List<WorkflowDetail>();

//                         var wfByName = workflows.ToDictionary(w => w.Workflow, w => w, StringComparer.OrdinalIgnoreCase);

//                         var edges = JsonSerializer.Deserialize<List<RelationshipEdge>>(relationshipsJson)
//                                    ?? new List<RelationshipEdge>();

//                         var rows = edges
//                             .Where(e => string.Equals(e.Type, "screen_to_workflow", StringComparison.OrdinalIgnoreCase))
//                             .Select(e =>
//                             {
//                                 var screen = ProgramLocalHelpers.StripPrefix(e.From, "screen:");
//                                 var workflow = ProgramLocalHelpers.StripPrefix(e.To, "workflow:");

//                                 var workflowDisplay = ProgramLocalHelpers.CleanWorkflowDisplay(workflow);
//                                 var workflowOut = workflowDisplay == workflow ? workflow : $"{workflowDisplay} ({workflow})";

//                                 wfByName.TryGetValue(workflow, out var wf);

//                                 var trigger = wf?.Trigger ?? "";
//                                 var purpose = wf?.Purpose ?? "";
//                                 var actions = wf?.ActionsDetected ?? new List<string>();

//                                 var evidenceFile = "";
//                                 var evidenceSnippet = "";
//                                 if (!string.IsNullOrWhiteSpace(e.Evidence))
//                                 {
//                                     var idx = e.Evidence.IndexOf(':');
//                                     if (idx >= 0)
//                                     {
//                                         evidenceFile = e.Evidence.Substring(0, idx).Trim();
//                                         evidenceSnippet = e.Evidence.Substring(idx + 1).Trim();
//                                     }
//                                     else
//                                     {
//                                         evidenceSnippet = e.Evidence.Trim();
//                                     }
//                                 }

//                                 if (evidenceSnippet.Length > 120) evidenceSnippet = evidenceSnippet.Substring(0, 120) + "...";

//                                 return new
//                                 {
//                                     Screen = screen,
//                                     Workflow = workflowOut,
//                                     Trigger = string.IsNullOrWhiteSpace(trigger) ? "Not found in uploaded files" : trigger,
//                                     Purpose = string.IsNullOrWhiteSpace(purpose) ? "Not found in uploaded files" : purpose,
//                                     Actions = actions.Count == 0 ? "Not found in uploaded files" : string.Join(", ", actions),
//                                     EvidenceFile = string.IsNullOrWhiteSpace(evidenceFile) ? "Not found in uploaded files" : evidenceFile,
//                                     EvidenceSnippet = string.IsNullOrWhiteSpace(evidenceSnippet) ? "Not found in uploaded files" : evidenceSnippet,
//                                 };
//                             })
//                             .ToList();

//                         var path = Path.Combine(outDir, "screen_workflow_mapping.md");

//                         if (rows.Count == 0)
//                         {
//                             File.WriteAllText(path, "Not found in uploaded files.", Encoding.UTF8);
//                             Console.WriteLine($"Wrote: {path}");
//                             return "";
//                         }

//                         var sb = new StringBuilder();
//                         sb.AppendLine("| Screen | Workflow | Trigger | Purpose | ActionsDetected | EvidenceFile | EvidenceSnippet |");
//                         sb.AppendLine("|---|---|---|---|---|---|---|");

//                         foreach (var r in rows)
//                         {
//                             sb.AppendLine($"| {ProgramLocalHelpers.EscapeMd(r.Screen)} | {ProgramLocalHelpers.EscapeMd(r.Workflow)} | {ProgramLocalHelpers.EscapeMd(r.Trigger)} | {ProgramLocalHelpers.EscapeMd(r.Purpose)} | {ProgramLocalHelpers.EscapeMd(r.Actions)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceFile)} | {ProgramLocalHelpers.EscapeMd(r.EvidenceSnippet)} |");
//                         }

//                         File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
//                         Console.WriteLine($"Wrote: {path}");

//                         //TODO: not sure if its word docx export
//                         wordPath = Path.Combine(outDir, "Replybrary_Diagrams.docx");
//                         Exporting.RunProcess("pandoc", $"\"{path}\" -o \"Replybrary_Diagrams.docx\" --toc", outDir);
//                         Console.WriteLine($"Wrote: {wordPath}");

//                         _jobs.setOutputFile(jobId, cmd, wordPath);
//                         //update job status to processing
//                         _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                     }


//                     var prompt =
//                         @"Create a Markdown table using ONLY:
//                         - relationships.json
//                         - workflows_detailed.json

//                         Goal:
//                         - Produce a SCREEN -> WORKFLOW mapping for ALL items where type == ""screen_to_workflow"".

//                         Rules:
//                         - Do NOT invent anything.
//                         - You MUST join relationships.json to workflows_detailed.json by workflow name.
//                         - Output ONLY the Markdown table (no explanation).
//                         - If there are zero screen_to_workflow items, output exactly: Not found in uploaded files.

//                         Table columns MUST be exactly:
//                         Screen | Workflow | Trigger | Purpose | ActionsDetected | EvidenceFile | EvidenceSnippet

//                         Formatting:
//                         - Screen: use the 'from' field but strip the leading 'screen:' if present (leave the rest)
//                         - Workflow: strip leading 'workflow:' if present
//                         - Trigger/Purpose/ActionsDetected: from workflows_detailed.json (actions join as comma-separated)
//                         - EvidenceFile: extract filename portion from evidence up to first ':' (e.g. 'Client Info Screen.fx.yaml')
//                         - EvidenceSnippet: include the '<FlowName>.Run(' snippet trimmed to ~120 chars, taken from evidence.
//                         ";

//                     var md = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     var aiPath = Path.Combine(outDir, "screen_workflow_mapping.md");
//                     File.WriteAllText(aiPath, md, Encoding.UTF8);
//                     Console.WriteLine($"Wrote: {aiPath}");

//                     //TODO: not sure if its word docx export
//                     wordPath = Path.Combine(outDir, "Replybrary_Screen_workflow_mapping.docx");
//                     Exporting.RunProcess("pandoc", $"\"{aiPath}\" -o \"Replybrary_Screen_workflow_mapping.docx\" --toc", outDir);
//                     Console.WriteLine($"Wrote: {wordPath}");

//                     _jobs.setOutputFile(jobId, cmd, wordPath);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                 }
//                 else if (cmd == "environment-variables")
//                 {
//                     var prompt =
//                         @"Create a json response for the solution using ONLY the specified structure to 
//                         capture environment variables and details such as type, description, name of the variable in Dev Environment, name of the variable in UAT Environment, name of the variable in Production Environment.
//                         For description fields, use brief text (1-2 sentences) taken from the uploaded chunks.
//                         If any information is missing like name, type, dev_value, test_value, or production_value, don't assume, use null for that field.
//                         Structure:
//                         [
//                         {
//                             ""Name"": ""<Environment Variable Name>"",
//                             ""Type"": ""<Type>"",
//                             ""Description"": ""<Description>"",
//                             ""DevValue"": ""<Dev Value - Name of Dev Environment - DEV>"",
//                             ""TestValue"": ""<Test Value - Name of UAT Environment - UAT>"",
//                             ""ProductionValue"": ""<Production Value - Name of Production Environment>""
//                         },
//                         ...
//                         ]
//                         ";

//                     Console.WriteLine("Generating environment-variables.xlsx...");

//                     var json_res = await OpenAIHttp.AskWithFileSearch(http, model, vsId, prompt);
//                     //construct table 

//                     var parsedJsonRes = JsonSerializer.Deserialize<HashSet<EnvironmentVariableValue>>(json_res);
//                     Console.WriteLine(parsedJsonRes.GetType());

//                     foreach (var item in parsedJsonRes)
//                     {
//                         try
//                         {
//                             var envVar = JsonSerializer.Deserialize<EnvironmentVariableValue>(item.ToString());
//                             if (envVar != null)
//                             {
//                                 if (envVar.DevValue == null)
//                                 {
//                                     envVar.DevValue = "<not specified>";
//                                 }

//                                 if (envVar.TestValue == null)
//                                 {
//                                     envVar.TestValue = "<not specified>";
//                                 }

//                                 if (envVar.ProductionValue == null)
//                                 {
//                                     envVar.ProductionValue = "<not specified>";
//                                 }

//                                 parsedJsonRes.Add(envVar);
//                             }
//                         }
//                         catch (JsonException)
//                         {
//                             Console.WriteLine($"Failed to parse item: {item}");
//                         }
//                     }

//                     //loop through parsedJsonRes for me
//                     foreach (var envVar in parsedJsonRes)
//                     {
//                         Console.WriteLine($"Name: {envVar.Name}");
//                         Console.WriteLine($"Type: {envVar.Type}");
//                         Console.WriteLine($"Description: {envVar.Description}");
//                         Console.WriteLine($"Dev Value: {envVar.DevValue}");
//                         Console.WriteLine($"Test Value: {envVar.TestValue}");
//                         Console.WriteLine($"Production Value: {envVar.ProductionValue}");
//                         Console.WriteLine();
//                     }

//                     var excel_path = Path.Combine(outDir, "environment-variables.xlsx");
//                     EnvironmentVariable.construct_table(parsedJsonRes, excel_path);

//                     _jobs.setOutputFile(jobId, cmd, excel_path);
//                     //update job status to processing
//                     _jobs.UpdateSingleOutputFileProgress(jobId, cmd, JobState.Completed);
//                     // return excel_path;

//                 } 
//                 //check azure test
//                 else
//                 {
//                     throw new Exception("Unknown generate type. Use: overview | workflows | faq | diagrams");
//                 }

//             }
//         }
//         catch (Exception ex)
//         {
//             Console.Error.WriteLine(ex.Message);
//             return "failed: " + ex.Message;
//             //Environment.Exit(1);
//         }
//         return response;

//     }
//     // helper: used by diagrams local mode to avoid "connect everything to everything"
//     static bool IsPowerAppsTriggered(string? trigger)
//     {
//         if (string.IsNullOrWhiteSpace(trigger)) return false;
//         return trigger.Contains("PowerAppV2", StringComparison.OrdinalIgnoreCase);
//     }
// }


// //TODO: check api before usage
// public class Validation
// {
//     public async Task<string?> checkAzureAI(string azureModel)
//     {
//         var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
//         var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

//         if (string.IsNullOrWhiteSpace(endpoint))
//             throw new Exception("Missing env var: AZURE_OPENAI_ENDPOINT");
//         if (string.IsNullOrWhiteSpace(key))
//             throw new Exception("Missing env var: AZURE_OPENAI_API_KEY");

//         using var az = new HttpClient { BaseAddress = new Uri(endpoint!) };
//         az.DefaultRequestHeaders.Add("api-key", key);

//         var payload = new
//         {
//             model = azureModel,
//             messages = new object[]
//             {
//                 new { role = "user", content = "What is the capital of France?" }
//             },
//             temperature = 0.0
//         };

//         var res = await az.PostAsync(
//             "chat/completions",
//             new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
//         );

//         var text = await res.Content.ReadAsStringAsync();
//         if (!res.IsSuccessStatusCode)
//             throw new Exception(text);

//         using var doc = JsonDocument.Parse(text);
//         var answer = doc.RootElement
//             .GetProperty("choices")[0]
//             .GetProperty("message")
//             .GetProperty("content")
//             .GetString();

//         Console.WriteLine(answer);
//         return "success";
//     }
// }

// // --------------------
// // Local JSON models (deterministic generation; avoids RAG retrieval failures)
// // --------------------
// internal sealed class WorkflowDetail
// {
//     [JsonPropertyName("workflow")]
//     public string Workflow { get; set; } = "";

//     [JsonPropertyName("file")]
//     public string File { get; set; } = "";

//     [JsonPropertyName("connectors")]
//     public List<string> Connectors { get; set; } = new();

//     [JsonPropertyName("env_vars_used")]
//     public List<string> EnvVarsUsed { get; set; } = new();

//     [JsonPropertyName("trigger")]
//     public string? Trigger { get; set; }

//     [JsonPropertyName("purpose")]
//     public string? Purpose { get; set; }

//     [JsonPropertyName("actions_detected")]
//     public List<string>? ActionsDetected { get; set; }
// }

// internal sealed class RelationshipEdge
// {
//     [JsonPropertyName("from")]
//     public string From { get; set; } = "";

//     [JsonPropertyName("to")]
//     public string To { get; set; } = "";

//     [JsonPropertyName("type")]
//     public string Type { get; set; } = "";

//     [JsonPropertyName("evidence")]
//     public string? Evidence { get; set; }
// }

// // Canvas app models for local diagrams/erd
// internal sealed class CanvasAppDetail
// {
//     [JsonPropertyName("app")]
//     public string App { get; set; } = "";

//     [JsonPropertyName("screens")]
//     public List<string> Screens { get; set; } = new();

//     [JsonPropertyName("connectors")]
//     public List<string> Connectors { get; set; } = new();

//     [JsonPropertyName("files_seen")]
//     public List<string> FilesSeen { get; set; } = new();
// }

// internal static partial class ProgramLocalHelpers
// {
//     public static string RequireChunksDir(string? chunksDir)
//     {
//         if (string.IsNullOrWhiteSpace(chunksDir) || !Directory.Exists(chunksDir))
//             throw new Exception("Missing or invalid --chunks. Example: --chunks \"/Users/daraling/Downloads/Replybrary_reports/chunks\"");
//         return chunksDir!;
//     }

//     public static string ReadChunksFile(string chunksDir, string fileName)
//     {
//         var path = Path.Combine(chunksDir, fileName);
//         if (!File.Exists(path)) throw new Exception($"Missing required chunk file: {path}");
//         return File.ReadAllText(path);
//     }

//     public static string StripPrefix(string s, string prefix)
//     {
//         if (s == null) return "";
//         return s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? s.Substring(prefix.Length) : s;
//     }

//     //  strip any common prefixes in relationships.json
//     public static string StripAnyKnownPrefix(string s)
//     {
//         if (string.IsNullOrWhiteSpace(s)) return "";
//         var prefixes = new[] { "app:", "screen:", "workflow:", "env:", "connector:" };
//         foreach (var p in prefixes)
//             s = StripPrefix(s, p);
//         return s;
//     }

//     public static string EscapeMd(string s)
//     {
//         if (s == null) return "";
//         return s.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|");
//     }

//     //  safe escaping for Mermaid labels (quoted strings)
//     public static string EscapeMermaidLabel(string s)
//     {
//         if (s == null) return "";
//         return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
//     }

//     // -------- helpers for clean workflow names + env var friendly names --------

//     public static string CleanWorkflowDisplay(string workflowName)
//     {
//         if (string.IsNullOrWhiteSpace(workflowName)) return workflowName;

//         var parts = workflowName.Split('-', 2);
//         if (parts.Length == 2 && LooksLikeGuidSuffix(parts[1]))
//             return parts[0];

//         return workflowName;
//     }

//     static bool LooksLikeGuidSuffix(string s)
//     {
//         return s.Count(c => c == '-') >= 4 && s.Any(char.IsDigit) && s.Any(char.IsLetter);
//     }

//     //  canvas app name cleaning for Mermaid labels
//     public static string CleanCanvasAppDisplay(string appName)
//     {
//         if (string.IsNullOrWhiteSpace(appName)) return appName;

//         // Example: wmreply_replybraryv2_c933c -> Replybraryv2 / Replybraryv2 (depending on source)
//         var s = appName;

//         if (s.StartsWith("wmreply_", StringComparison.OrdinalIgnoreCase))
//             s = s.Substring("wmreply_".Length);

//         // remove trailing _xxxxx (<=8 chars) if present
//         var lastUnderscore = s.LastIndexOf('_');
//         if (lastUnderscore > 0 && (s.Length - lastUnderscore) <= 8)
//             s = s.Substring(0, lastUnderscore);

//         s = s.Replace("_", " ");

//         // Title-case-ish per word
//         var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//         for (int i = 0; i < parts.Length; i++)
//         {
//             var p = parts[i];
//             if (p.Length == 0) continue;
//             parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
//         }
//         return string.Join(" ", parts);
//     }

//     public static Dictionary<string, string> LoadEnvMap(string? envMapPath)
//     {
//         if (string.IsNullOrWhiteSpace(envMapPath))
//             return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

//         if (!File.Exists(envMapPath))
//             throw new Exception($"envmap file not found: {envMapPath}");

//         var json = File.ReadAllText(envMapPath);

//         var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
//                    ?? new Dictionary<string, string>();

//         return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
//     }

//     public static string EnvDisplay(string envVar, Dictionary<string, string> map)
//     {
//         if (string.IsNullOrWhiteSpace(envVar)) return envVar;
//         if (map.TryGetValue(envVar, out var nice) && !string.IsNullOrWhiteSpace(nice))
//             return $"{nice} ({envVar})";
//         return envVar;
//     }

//     public static string EnvMapAsBulletText(Dictionary<string, string> map)
//     {
//         if (map == null || map.Count == 0) return "(none)";
//         var sb = new StringBuilder();
//         foreach (var kv in map.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
//             sb.AppendLine($"- {kv.Value} ({kv.Key})");
//         return sb.ToString();
//     }

//     //env var extraction for local diagrams/erd (envvars.json isn’t a plain string array)
//     public static List<string> ExtractEnvVarNamesFromEnvVarsJson(string envvarsJson)
//     {
//         var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//         if (string.IsNullOrWhiteSpace(envvarsJson)) return found.ToList();

//         var token = "wmreply_";
//         int i = 0;
//         while (i < envvarsJson.Length)
//         {
//             var idx = envvarsJson.IndexOf(token, i, StringComparison.OrdinalIgnoreCase);
//             if (idx < 0) break;

//             int end = idx;
//             while (end < envvarsJson.Length)
//             {
//                 char c = envvarsJson[end];
//                 if (char.IsLetterOrDigit(c) || c == '_') end++;
//                 else break;
//             }

//             var name = envvarsJson.Substring(idx, end - idx).Trim();
//             if (!string.IsNullOrWhiteSpace(name))
//                 found.Add(name);

//             i = end;
//         }

//         return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
//     }
// }
