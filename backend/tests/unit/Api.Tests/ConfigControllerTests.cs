using System.Text.Json;
using backend.Api.Controllers;
using backend.Application.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Tests;

public class ConfigControllerTests
{
    [Fact]
    public void GetShared_ReturnsOk_WithBackendUrl_AiModels_CustomLimit_AndGeneratedTypes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:customPromptCharacterLimit"] = "200",
                ["Frontend:GeneratedOutputTypes:Overview:id"] = "overview",
                ["Frontend:GeneratedOutputTypes:Overview:description"] = "Overview description",
                ["Frontend:GeneratedOutputTypes:Custom document:id"] = "custom-document",
                ["Frontend:GeneratedOutputTypes:Custom document:description"] = "Custom",
            })
            .Build();

        var shared = new SharedOptions
        {
            BackendUrl = "http://api.test",
            AIModels = new Dictionary<string, string[]>
            {
                ["openai"] = ["gpt-a", "gpt-b"],
            },
        };

        var controller = new ConfigController(Options.Create(shared));

        var result = controller.GetShared(configuration);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("http://api.test", root.GetProperty("backendUrl").GetString());
        Assert.Equal(200, root.GetProperty("customPromptCharacterLimit").GetInt32());

        var models = root.GetProperty("aiModels");
        Assert.Equal(JsonValueKind.Array, models.ValueKind);
        Assert.Equal(2, models.GetArrayLength());

        var types = root.GetProperty("generatedOutputTypes");
        Assert.Equal(JsonValueKind.Array, types.ValueKind);
        Assert.Equal(2, types.GetArrayLength());
        // custom-document last
        Assert.Equal("custom-document", types[1].GetProperty("id").GetString());
    }
}
