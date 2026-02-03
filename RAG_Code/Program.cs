
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

static string MustEnv(string name)
{
    var v = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(v))
        throw new Exception($"Missing env var: {name}");
    return v!;
}

static async Task<JsonElement> ReadJson(HttpResponseMessage res)
{
    var text = await res.Content.ReadAsStringAsync();
    if (!res.IsSuccessStatusCode)
        throw new Exception($"HTTP {(int)res.StatusCode}:\n{text}");
    return JsonDocument.Parse(text).RootElement;
}

static async Task<string> CreateVectorStore(HttpClient http, string name)
{
    var body = JsonSerializer.Serialize(new { name });
    var res = await http.PostAsync(
        "https://api.openai.com/v1/vector_stores",
        new StringContent(body, Encoding.UTF8, "application/json")
    );
    var json = await ReadJson(res);
    return json.GetProperty("id").GetString()!;
}

static async Task<string> UploadFile(HttpClient http, string path)
{
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent("assistants"), "purpose");

    await using var fs = File.OpenRead(path);
    var fileContent = new StreamContent(fs);
    fileContent.Headers.ContentType =
        new MediaTypeHeaderValue("application/octet-stream");

    form.Add(fileContent, "file", Path.GetFileName(path));

    var res = await http.PostAsync("https://api.openai.com/v1/files", form);
    var json = await ReadJson(res);
    return json.GetProperty("id").GetString()!;
}

static async Task AttachFileToVectorStore(HttpClient http, string vectorStoreId, string fileId)
{
    var body = JsonSerializer.Serialize(new { file_id = fileId });
    var res = await http.PostAsync(
        $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files",
        new StringContent(body, Encoding.UTF8, "application/json")
    );
    _ = await ReadJson(res);
}

static async Task<string> AskWithFileSearch(HttpClient http, string model, string vectorStoreId, string prompt)
{
    var payload = new
    {
        model,
        input = prompt,
        tools = new object[]
        {
            new {
                type = "file_search",
                vector_store_ids = new[] { vectorStoreId }
            }
        }
    };

    var body = JsonSerializer.Serialize(payload);
    var res = await http.PostAsync(
        "https://api.openai.com/v1/responses",
        new StringContent(body, Encoding.UTF8, "application/json")
    );

    var json = await ReadJson(res);

    if (json.TryGetProperty("output_text", out var ot) &&
        ot.ValueKind == JsonValueKind.String)
        return ot.GetString()!;

    if (json.TryGetProperty("output", out var output) &&
        output.ValueKind == JsonValueKind.Array)
    {
        foreach (var item in output.EnumerateArray())
        {
            if (item.TryGetProperty("type", out var t) &&
                t.GetString() == "message" &&
                item.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in content.EnumerateArray())
                {
                    if (c.TryGetProperty("type", out var ct) &&
                        ct.GetString() == "output_text" &&
                        c.TryGetProperty("text", out var tx))
                        return tx.GetString()!;
                }
            }
        }
    }

    return json.ToString();
}

static void PrintUsage()
{
    Console.WriteLine(@"
Usage:

  dotnet run -- index --chunks ""<chunks_folder>"" [--name <vector_store_name>]

  dotnet run -- ask ""<question>"" [--vs <vector_store_id>] [--model <model>]

  dotnet run -- generate overview        [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate workflows       [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate faq             [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate diagrams        [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate erd             [--out <folder>] [--vs <vector_store_id>] [--model <model>]
  dotnet run -- generate screen-mapping  [--out <folder>] [--vs <vector_store_id>] [--model <model>]

  dotnet run -- export word [--out <folder>]
  dotnet run -- export pdf  [--out <folder>]

  dotnet run -- demo [--out <folder>]

Notes:
- index = one-time cost (uploads + embeddings)
- ask / generate = cheap (reuse vector store)
- export requires pandoc installed (brew install pandoc)
");
}

static int RunProcess(string fileName, string arguments, string workingDir)
{
    var p = new Process();
    p.StartInfo.FileName = fileName;
    p.StartInfo.Arguments = arguments;
    p.StartInfo.WorkingDirectory = workingDir;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.RedirectStandardError = true;
    p.StartInfo.UseShellExecute = false;

    p.Start();
    var stdout = p.StandardOutput.ReadToEnd();
    var stderr = p.StandardError.ReadToEnd();
    p.WaitForExit();

    if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout.Trim());
    if (p.ExitCode != 0)
        throw new Exception($"Command failed: {fileName} {arguments}\n{stderr}");

    return p.ExitCode;
}

//  small router so "ask" looks in the right chunk
static string BuildRoutedPrompt(string question)
{
    var q = question.ToLowerInvariant();

    bool isEdges =
        q.Contains("edge") ||
        q.Contains("edges") ||
        q.Contains("mapping") ||
        q.Contains("map") ||
        q.Contains("relationship") ||
        q.Contains("relationships") ||
        q.Contains("screen_to_workflow") ||
        q.Contains("workflow_to_env") ||
        q.Contains("app_to_screen") ||
        q.Contains("app_to_connector") ||
        q.Contains("workflow_to_connector") ||
        q.Contains("connects") ||
        q.Contains("links");

    bool isErd =
        q.Contains("erd") ||
        q.Contains("entity relationship") ||
        q.Contains("er diagram") ||
        q.Contains("schema") ||
        q.Contains("tables") ||
        q.Contains("fields") ||
        q.Contains("columns") ||
        q.Contains("primary key") ||
        q.Contains("foreign key");

    var sb = new StringBuilder();

    sb.AppendLine("Answer using ONLY the uploaded solution chunks.");
    sb.AppendLine("If the information is not present, say: Not found in uploaded files.");
    sb.AppendLine();

    if (isEdges)
    {
        sb.AppendLine("Routing rule:");
        sb.AppendLine("- You MUST look in the uploaded file named relationships.json when the question is about edges, mappings, or relationships.");
        sb.AppendLine();
    }

    if (isErd)
    {
        sb.AppendLine("Routing rule:");
        sb.AppendLine("- You MUST look in the uploaded file named erd_schema.json when the question is about ERD/schema/tables/fields.");
        sb.AppendLine("- Do NOT invent tables, fields, or relationships.");
        sb.AppendLine();
    }

    sb.AppendLine("Question:");
    sb.AppendLine(question);

    return sb.ToString();
}

// ---------------- MAIN ----------------

try
{
    var apiKey = MustEnv("OPENAI_API_KEY");

    // Put current vector store id here (the one i have created and reused)
    // NOTE: you can override at runtime via --vs
    string defaultVs = "vs_6972909f03948191a19a88a9fd13e234";
    string vsId = defaultVs;

    // Cheap model by default; override with --model if needed
    string model = "gpt-5-mini";

    string outDir = Path.Combine(Directory.GetCurrentDirectory(), "rag_outputs");

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", apiKey);

    if (args.Length == 0)
    {
        PrintUsage();
        return;
    }

    string? GetFlag(string name)
    {
        for (int i = 0; i < args.Length; i++)
            if (args[i] == name && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }

    var vsFlag = GetFlag("--vs");
    if (!string.IsNullOrWhiteSpace(vsFlag)) vsId = vsFlag!;
    var modelFlag = GetFlag("--model");
    if (!string.IsNullOrWhiteSpace(modelFlag)) model = modelFlag!;
    var outFlag = GetFlag("--out");
    if (!string.IsNullOrWhiteSpace(outFlag)) outDir = outFlag!;

    Directory.CreateDirectory(outDir);

    var cmd = args[0].ToLowerInvariant();

    // -------- index --------
    if (cmd == "index")
    {
        var chunks = GetFlag("--chunks");
        var name = GetFlag("--name") ?? "replybrary_chunks";

        if (string.IsNullOrWhiteSpace(chunks) || !Directory.Exists(chunks))
            throw new Exception("index requires: --chunks \"<path>\"");

        Console.WriteLine("Creating vector store...");
        var newVs = await CreateVectorStore(http, name);
        Console.WriteLine($"Vector store: {newVs}");

        var files = Directory.GetFiles(chunks!, "*.json", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} json files.");

        foreach (var f in files)
        {
            Console.WriteLine($"Uploading: {Path.GetFileName(f)}");
            var fileId = await UploadFile(http, f);
            await AttachFileToVectorStore(http, newVs, fileId);
            Console.WriteLine($"  attached file_id={fileId}");
        }

        Console.WriteLine("Done. Save this vector store id and reuse it:");
        Console.WriteLine(newVs);
        return;
    }

    // -------- ask --------
    if (cmd == "ask")
    {
        if (args.Length < 2)
            throw new Exception("ask requires a question: dotnet run -- ask \"...\"");

        var question = string.Join(" ", args.Skip(1));
        var prompt = BuildRoutedPrompt(question);

        var answer = await AskWithFileSearch(http, model, vsId, prompt);
        Console.WriteLine(answer);
        return;
    }

    // -------- generate --------
    if (cmd == "generate")
    {
        if (args.Length < 2)
            throw new Exception("generate requires a type: overview | workflows | faq | diagrams | erd | screen-mapping");

        var kind = args[1].ToLowerInvariant();

        if (kind == "overview")
        {
            var prompt =
@"Generate a clean Markdown solution overview based ONLY on uploaded chunks.
Include:
- Counts: canvas apps, workflows, env vars, relationship edges (by type), screens (if present)
- Connectors used (unique list)
- Workflows list
- Env var names list
- If screens exist: list screens per app (names)
Keep it concise with headings + bullet points.";

            var md = await AskWithFileSearch(http, model, vsId, prompt);
            var path = Path.Combine(outDir, "overview.md");
            File.WriteAllText(path, md, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        if (kind == "workflows")
        {
            var prompt =
@"Summarise each workflow in Markdown using ONLY the uploaded chunks.
For each workflow include:
- Workflow name
- What it does (1–3 lines)
- Any obvious trigger/purpose if available
If missing details, say 'Not found in uploaded files.'";

            var md = await AskWithFileSearch(http, model, vsId, prompt);
            var path = Path.Combine(outDir, "workflows.md");
            File.WriteAllText(path, md, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        if (kind == "faq")
        {
            var prompt =
@"Create a Markdown FAQ for the solution using ONLY the uploaded chunks.
Include ~10 Q&As (workflows, env vars, canvas apps, what the solution contains).
If info is missing, say 'Not found in uploaded files.' Keep it concise.";

            var md = await AskWithFileSearch(http, model, vsId, prompt);
            var path = Path.Combine(outDir, "faq.md");
            File.WriteAllText(path, md, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        if (kind == "diagrams")
        {
            var prompt =
@"Output ONLY ONE Mermaid diagram (no explanation), using flowchart LR.

It MUST include all three groups as explicit nodes:
1) Canvas Apps (both apps as nodes)
2) Workflows (each workflow as its own node — do NOT use a single 'Workflows' hub node)
3) Environment Variables (each env var as its own node)

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
- Labels must use the real names from the uploaded chunks.
- Output ONLY valid Mermaid code. No second diagram. No markdown fences.";

            var mermaid = await AskWithFileSearch(http, model, vsId, prompt);
            mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

            var path = Path.Combine(outDir, "architecture.mmd");
            File.WriteAllText(path, mermaid, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        // ERD generator
        if (kind == "erd")
        {
            var prompt =
@"Output ONLY Mermaid erDiagram code (no markdown fences, no explanation).

Use ONLY the uploaded file named erd_schema.json.
Do NOT invent tables, fields, or relationships.

Rules:
- If erd_schema.json has no tables, output an erDiagram with a single comment line explaining it's empty.
- For each table: include its fields.
- Only include a field if it exists in erd_schema.json.
- Keep field types simple (string, int, decimal, bool, datetime, guid) based on schema.
- Use relationship types exactly:
  - 1:N shown as: A ||--o{ B : ""relationship_name""
  - N:N shown as: A }o--o{ B : ""relationship_name""
- If no relationships exist in erd_schema.json, output an erDiagram with tables only and no edges.
- Use logical names if display names are missing.";

            var mermaid = await AskWithFileSearch(http, model, vsId, prompt);
            mermaid = mermaid.Replace("```mermaid", "").Replace("```", "").Trim();

            var path = Path.Combine(outDir, "erd.mmd");
            File.WriteAllText(path, mermaid, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        // SCREEN->WORKFLOW mapping markdown 
        if (kind == "screen-mapping")
        {
            var prompt =
@"Create a Markdown table using ONLY the uploaded file named relationships.json.

Goal:
- Produce a SCREEN -> WORKFLOW mapping table for ALL items where type == ""screen_to_workflow"".

Rules:
- Do NOT invent anything.
- Table columns MUST be exactly: Screen | Workflow | EvidenceFile | EvidenceSnippet
- EvidenceFile: extract the filename portion from evidence (up to the first ':', e.g. ""Client Info Screen.fx.yaml"")
- EvidenceSnippet: include a short snippet that contains the ""<FlowName>.Run("" call (trim to ~120 chars)
- If there are zero screen_to_workflow items, output: ""Not found in uploaded files.""
- Output ONLY the Markdown table (no explanation).";

            var md = await AskWithFileSearch(http, model, vsId, prompt);
            var path = Path.Combine(outDir, "screen_workflow_mapping.md");
            File.WriteAllText(path, md, Encoding.UTF8);
            Console.WriteLine($"Wrote: {path}");
            return;
        }

        throw new Exception("Unknown generate type. Use: overview | workflows | faq | diagrams | erd | screen-mapping");
    }

    // -------- export --------
    if (cmd == "export")
    {
        if (args.Length < 2)
            throw new Exception("export requires: word | pdf");

        var kind = args[1].ToLowerInvariant();

        var overview = Path.Combine(outDir, "overview.md");
        var workflows = Path.Combine(outDir, "workflows.md");
        var faq = Path.Combine(outDir, "faq.md");

        if (!File.Exists(overview) || !File.Exists(workflows) || !File.Exists(faq))
            throw new Exception($"Missing markdown files in {outDir}. Run generate first.");

        if (kind == "word")
        {
            RunProcess("pandoc", $"\"{overview}\" -o \"Replybrary_Overview.docx\" --toc", outDir);
            RunProcess("pandoc", $"\"{workflows}\" -o \"Replybrary_Workflows.docx\" --toc", outDir);
            RunProcess("pandoc", $"\"{faq}\" -o \"Replybrary_FAQ.docx\" --toc", outDir);

            // Optional: generated mapping/erd, export them too (if missing this wont fail) 
            var map = Path.Combine(outDir, "screen_workflow_mapping.md");
            if (File.Exists(map))
                RunProcess("pandoc", $"\"{map}\" -o \"Replybrary_Screen_Workflow_Mapping.docx\" --toc", outDir);

            var erd = Path.Combine(outDir, "erd.mmd");
            if (File.Exists(erd))
                RunProcess("pandoc", $"\"{erd}\" -o \"Replybrary_ERD_Mermaid.docx\" --toc", outDir);

            Console.WriteLine("Wrote Word docs into: " + outDir);
            return;
        }

        if (kind == "pdf")
        {
            RunProcess("pandoc", $"\"{overview}\" -o \"Replybrary_Overview.pdf\" --toc", outDir);
            RunProcess("pandoc", $"\"{workflows}\" -o \"Replybrary_Workflows.pdf\" --toc", outDir);
            RunProcess("pandoc", $"\"{faq}\" -o \"Replybrary_FAQ.pdf\" --toc", outDir);

            var map = Path.Combine(outDir, "screen_workflow_mapping.md");
            if (File.Exists(map))
                RunProcess("pandoc", $"\"{map}\" -o \"Replybrary_Screen_Workflow_Mapping.pdf\" --toc", outDir);

            var erd = Path.Combine(outDir, "erd.mmd");
            if (File.Exists(erd))
                RunProcess("pandoc", $"\"{erd}\" -o \"Replybrary_ERD_Mermaid.pdf\" --toc", outDir);

            Console.WriteLine("Wrote PDFs into: " + outDir);
            return;
        }

        throw new Exception("export requires: word | pdf");
    }

    // -------- demo --------
    if (cmd == "demo")
    {
        Console.WriteLine("Demo outputs folder:");
        Console.WriteLine(outDir);
        Console.WriteLine();
        Console.WriteLine("Recommended demo run:");
        Console.WriteLine("  dotnet run -- generate overview");
        Console.WriteLine("  dotnet run -- generate workflows");
        Console.WriteLine("  dotnet run -- generate faq");
        Console.WriteLine("  dotnet run -- generate diagrams");
        Console.WriteLine("  dotnet run -- generate screen-mapping");
        Console.WriteLine("  dotnet run -- generate erd        (requires erd_schema.json in chunks + indexed)");
        Console.WriteLine("  dotnet run -- export word   (requires pandoc)");
        Console.WriteLine("  dotnet run -- export pdf    (requires pandoc)");
        Console.WriteLine();
        Console.WriteLine("Open outputs in Finder:");
        Console.WriteLine($"  open \"{outDir}\"");
        return;
    }

    PrintUsage();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}