using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RagCliApp;

public static class OpenAIHttp
{
    public static async Task<JsonElement> ReadJson(HttpResponseMessage res)
    {
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"HTTP {(int)res.StatusCode}:\n{text}");
        return JsonDocument.Parse(text).RootElement;
    }

<<<<<<< HEAD
=======
    /// <summary>
    /// Lightweight connectivity check: issues a simple GET against the base URL.
    /// This is used only for LLM health validation and does not depend on a specific model.
    /// </summary>
    public static async Task PingAsync(HttpClient http)
    {
        // Try the bare base URL first
        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync("");
        }
        catch (Exception ex)
        {
            throw new Exception($"LLM endpoint not reachable: {ex.Message}", ex);
        }

        if (!res.IsSuccessStatusCode)
        {
            var text = res.Content == null ? "" : await res.Content.ReadAsStringAsync();
            throw new Exception(
                $"LLM health check failed: HTTP {(int)res.StatusCode} {res.ReasonPhrase}\n{text}"
            );
        }
    }
    
    //new
>>>>>>> 3677cb5 (feat: add LLM health check upon starting + disable not working model in dropdown)
    public static async Task<string> CreateVectorStore(HttpClient http, string name)
    {
        var body = JsonSerializer.Serialize(new { name });
        var res = await http.PostAsync(
            "https://api.openai.com/v1/vector_stores",
            new StringContent(body, Encoding.UTF8, "application/json")
        );
        var json = await ReadJson(res);
        return json.GetProperty("id").GetString()!;
    }

    public static async Task<string> UploadFile(HttpClient http, string path)
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

    public static async Task AttachFileToVectorStore(HttpClient http, string vectorStoreId, string fileId)
    {
        var body = JsonSerializer.Serialize(new { file_id = fileId });
        var res = await http.PostAsync(
            $"https://api.openai.com/v1/vector_stores/{vectorStoreId}/files",
            new StringContent(body, Encoding.UTF8, "application/json")
        );
        _ = await ReadJson(res);
    }

    public static async Task<string> AskWithFileSearch(HttpClient http, string model, string vectorStoreId, string prompt)
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

    /// <summary>
    /// Tiny test call against the Responses API to validate that a specific model/deployment exists.
    /// Used by LlmHealth to distinguish working vs broken deployments.
    /// </summary>
    public static async Task TestModelAsync(HttpClient http, string model)
    {
        var payload = new
        {
            model,
            input = "ping",
        };

        var body = JsonSerializer.Serialize(payload);
        var res = await http.PostAsync("responses", new StringContent(body, Encoding.UTF8, "application/json"));

        // Will throw with detailed body (including DeploymentNotFound) if not OK
        _ = await ReadJson(res);
    }
}
