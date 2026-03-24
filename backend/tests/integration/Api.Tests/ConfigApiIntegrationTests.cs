using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.IntegrationTests;

public class ConfigApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConfigApiIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task GetShared_Returns200_AndJsonWithExpectedShape()
    {
        var response = await _client.GetAsync("/api/config/shared");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("backendUrl", out var backendUrl));
        Assert.False(string.IsNullOrWhiteSpace(backendUrl.GetString()));

        Assert.True(root.TryGetProperty("aiModels", out var models));
        Assert.Equal(JsonValueKind.Array, models.ValueKind);

        Assert.True(root.TryGetProperty("customPromptCharacterLimit", out var limit));
        Assert.True(limit.TryGetInt32(out _));

        Assert.True(root.TryGetProperty("generatedOutputTypes", out var types));
        Assert.Equal(JsonValueKind.Array, types.ValueKind);
    }

    [Fact]
    public async Task GetLlmStatus_Returns200_AndJsonArray()
    {
        var response = await _client.GetAsync("/api/config/llm-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}
