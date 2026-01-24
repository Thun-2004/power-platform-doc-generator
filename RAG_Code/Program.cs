using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

static string MustEnv(string name)
{
    var v = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(v)) throw new Exception($"Missing env var: {name}");
    return v;
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
    // POST /v1/vector_stores
    var body = JsonSerializer.Serialize(new { name });
    var res = await http.PostAsync("https://api.openai.com/v1/vector_stores",
        new StringContent(body, Encoding.UTF8, "application/json"));
    var json = await ReadJson(res);
    return json.GetProperty("id").GetString()!;
}

static async Task<string> UploadFile(HttpClient http, string path)
{
    // POST /v1/files  (purpose=assistants)
    using var form = new MultipartFormDataContent();
    form.Add(new StringContent("assistants"), "purpose");

    await using var fs = File.OpenRead(path);
    var fileContent = new StreamContent(fs);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    form.Add(fileContent, "file", Path.GetFileName(path));

    var res = await http.PostAsync("https://api.openai.com/v1/files", form);
    var json = await ReadJson(res);
    return json.GetProperty("id").GetString()!;
}

static async Task AttachFileToVectorStore(HttpClient http, string vectorStoreId, string fileId)
{
    // POST /v1/vector_stores/{vector_store_id}/files
    var body = JsonSerializer.Serialize(new { file_id = fileId });
    var res = await http.PostAsync(
        $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files",
        new StringContent(body, Encoding.UTF8, "application/json"));
    _ = await ReadJson(res);
}

static async Task<string> AskWithFileSearch(HttpClient http, string model, string vectorStoreId, string question)
{
    // POST /v1/responses with tool=file_search + vector_store_ids
    var payload = new
    {
        model,
        input = question,
        tools = new object[]
        {
            new {
                type = "file_search",
                vector_store_ids = new[] { vectorStoreId }
            }
        }
    };

    var body = JsonSerializer.Serialize(payload);
    var res = await http.PostAsync("https://api.openai.com/v1/responses",
        new StringContent(body, Encoding.UTF8, "application/json"));
    var json = await ReadJson(res);

   
    if (json.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
        return ot.GetString()!;

    // fallback: just return whole json
    return json.ToString();
}

try
{
    var apiKey = MustEnv("OPENAI_API_KEY");

    // Args: --chunks "<path>" --q "<question>" (both optional)
    string chunksPath = "/Users/daraling/Downloads/Replybrary_reports/chunks";
    string question = "How many workflows are in this solution? List their names.";
    string model = "gpt-5-mini"; // cheap for dev; change to "gpt-5.2-chat" extra impact later ask Grant

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--chunks" && i + 1 < args.Length) chunksPath = args[++i];
        else if (args[i] == "--q" && i + 1 < args.Length) question = args[++i];
        else if (args[i] == "--model" && i + 1 < args.Length) model = args[++i];
    }

    if (!Directory.Exists(chunksPath))
        throw new Exception($"Chunks folder not found: {chunksPath}");

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

    Console.WriteLine("Creating vector store...");
    var vsId = await CreateVectorStore(http, "replybrary_chunks");
    Console.WriteLine($"Vector store: {vsId}");

    var files = Directory.GetFiles(chunksPath, "*.json", SearchOption.AllDirectories);
    Console.WriteLine($"Found {files.Length} json files.");

    // Upload + attach
    foreach (var f in files)
    {
        Console.WriteLine($"Uploading: {Path.GetFileName(f)}");
        var fileId = await UploadFile(http, f);
        await AttachFileToVectorStore(http, vsId, fileId);
        Console.WriteLine($"  attached file_id={fileId}");
    }

    
    Console.WriteLine("Waiting briefly for indexing...");
    await Task.Delay(3000);

    Console.WriteLine("\nAsking with file_search...");
    var answer = await AskWithFileSearch(http, model, vsId, question);
    Console.WriteLine("\nANSWER:\n" + answer);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}