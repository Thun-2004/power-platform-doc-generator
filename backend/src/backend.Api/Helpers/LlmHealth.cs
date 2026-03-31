using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Application.Config;
using backend.Application.Helpers;
using backend.Application.LLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace backend.Api.Helpers;

public record LlmModelStatus(
    string Model,
    string Provider,
    string BaseUrl,
    bool IsHealthy,
    string? Error
);

public static class LlmHealth
{
    public static async Task<IReadOnlyList<LlmModelStatus>> GetStatusesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var llm = scope.ServiceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
        var shared = scope.ServiceProvider.GetService<IOptions<SharedOptions>>()?.Value;

        var results = new List<LlmModelStatus>();

        // Map provider -> models from SharedOptions.AIModels if available
        var modelsByProvider = shared?.AIModels ?? new Dictionary<string, string[]>();

        // Validate env vars/base URLs per provider and also test each configured model/deployment
        foreach (var kvp in llm.LLMKeys)
        {
            var provider = kvp.Key;
            var envKeyName = kvp.Value;

            try
            {
                string baseUrl;
                var apiKey = EnvReader.Load(envKeyName);

                if (!llm.LLMUrls.TryGetValue(provider, out baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
                    throw new Exception($"Missing LLM base URL for provider '{provider}'.");

                var cfg = new OpenAiApiConfig { Provider = provider, BaseUrl = baseUrl, ApiKey = apiKey, TimeoutMinutes = 1 };
                using var client = OpenAIHttp.CreateClient(cfg);

                // Emit a status for each model belonging to this provider, or at least the provider name
                if (modelsByProvider.TryGetValue(provider, out var models) && models is { Length: > 0 })
                {
                    foreach (var modelName in models)
                    {
                        bool modelHealthy;
                        string? modelError = null;
                        try
                        {
                            // Tiny test call per model/deployment -> DeploymentNotFound
                            await OpenAIHttp.TestModelAsync(client, modelName);
                            modelHealthy = true;
                        }
                        catch (Exception ex)
                        {
                            modelError = ex.Message;

                            // Only mark model unhealthy for real deployment/auth problems.
                            // avoids false-negatives when the health-call payload differs
                            // from the real generation payload.
                            var msg = ex.Message ?? string.Empty;
                            var isDeploymentMissing = msg.Contains("DeploymentNotFound", StringComparison.OrdinalIgnoreCase);
                            var isAuthFailed =
                                msg.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                                msg.Contains("403", StringComparison.OrdinalIgnoreCase) ||
                                msg.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                msg.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);

                            modelHealthy = !(isDeploymentMissing || isAuthFailed);
                        }

                        results.Add(new LlmModelStatus(modelName, provider, baseUrl, modelHealthy, modelError));
                    }
                }
                else
                {
                    // No explicit models; just test base URL once.
                    bool ok;
                    string? err = null;
                    try
                    {
                        await OpenAIHttp.PingAsync(client);
                        ok = true;
                    }
                    catch (Exception ex)
                    {
                        ok = false;
                        err = ex.Message;
                    }

                    results.Add(new LlmModelStatus(provider, provider, baseUrl, ok, err));
                }
            }
            catch (Exception ex)
            {
                var baseUrl = llm.LLMUrls.GetValueOrDefault(provider) ?? string.Empty;

                if (modelsByProvider.TryGetValue(provider, out var models) && models is { Length: > 0 })
                {
                    foreach (var modelName in models)
                    {
                        results.Add(new LlmModelStatus(modelName, provider, baseUrl, false, ex.Message));
                    }
                }
                else
                {
                    results.Add(new LlmModelStatus(provider, provider, baseUrl, false, ex.Message));
                }
            }
        }

        return results;
    }
}

