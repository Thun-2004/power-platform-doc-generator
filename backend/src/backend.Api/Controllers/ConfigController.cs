using System;
using System.Collections.Generic;
using System.Linq;
using backend.Application.Config;
using backend.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly SharedOptions _shared;

    public ConfigController(IOptions<SharedOptions> shared)
    {
        _shared = shared.Value;
    }

    [HttpGet("shared")]
    public IActionResult GetShared([FromServices] IConfiguration configuration)
    {
        var aiModelsFlat = (_shared.AIModels ?? new Dictionary<string, string[]>() )
            .SelectMany(kv => kv.Value ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var customPromptCharacterLimit =
            configuration.GetValue<int?>("Frontend:customPromptCharacterLimit") ?? 250;

        // Generated output types are stored under Frontend:GeneratedOutputTypes in appsettings.json
        // Example shape:
        // "Overview": { "id": "overview", "description": "..." }
        var generatedOutputTypesSection =
            configuration.GetSection("Frontend:GeneratedOutputTypes");

        var generatedOutputTypesDict =
            generatedOutputTypesSection.Get<Dictionary<string, GeneratedOutputTypeDto>>() ??
            new Dictionary<string, GeneratedOutputTypeDto>();

        var generatedOutputTypes = generatedOutputTypesDict
            .Where(kv => kv.Value != null)
            .Select(kv => new
            {
                id = kv.Value!.id,
                title = kv.Key,
                desc = kv.Value.description
            })
            .ToArray();

        return Ok(new
        {
            backendUrl = _shared.BackendUrl,
            aiModels = aiModelsFlat,
            customPromptCharacterLimit,
            generatedOutputTypes
        });
    }

    [HttpGet("llm-status")]
    public async Task<IActionResult> GetLlmStatus([FromServices] IServiceProvider services)
    {
        var statuses = await LlmHealth.GetStatusesAsync(services);

        return Ok(statuses.Select(s => new
        {
            model = s.Model,
            provider = s.Provider,
            baseUrl = s.BaseUrl,
            isHealthy = s.IsHealthy,
            error = s.Error
        }));
    }
}

public record GeneratedOutputTypeDto(string id, string description);

