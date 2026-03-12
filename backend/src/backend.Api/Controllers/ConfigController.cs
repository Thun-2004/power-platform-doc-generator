using System;
using System.Collections.Generic;
using System.Linq;
using backend.Application.Config;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult GetShared()
    {
        var aiModelsFlat = (_shared.AIModels ?? new Dictionary<string, string[]>() )
            .SelectMany(kv => kv.Value ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new
        {
            backendUrl = _shared.BackendUrl,
            aiModels = aiModelsFlat
        });
    }
}

