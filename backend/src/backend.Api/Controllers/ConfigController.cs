using System;
using System.Collections.Generic;
using System.Linq;
using backend.Application.Config;
using backend.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using backend.Api.Helpers;

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
    public async Task<IActionResult> GetShared([FromServices] IConfiguration configuration, [FromServices] IServiceProvider services)
    {
      
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

        // Dictionary binding does not guarantee JSON key order; keep "custom-document" last in the list.
        var generatedOutputTypesList = generatedOutputTypesDict
            .Where(kv => kv.Value != null)
            .Select(kv => new
            {
                id = kv.Value!.id,
                title = kv.Key,
                desc = kv.Value.description
            })
            .ToList();
        // Keep custom-document at the end of the list.
        var customDoc = generatedOutputTypesList.FirstOrDefault(x => x.id == "custom-document");
        var ordered = generatedOutputTypesList.Where(x => x.id != "custom-document").ToList();
        if (customDoc != null)
            ordered.Add(customDoc);
        var generatedOutputTypes = ordered.ToArray();

        return Ok(new
        {
            backendUrl = _shared.BackendUrl,
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

