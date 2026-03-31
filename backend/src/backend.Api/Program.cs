using Scalar.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DotNetEnv;

using backend.Domain;
using backend.Infrastructure;
using backend.Application;
using backend.Application.Config;
using backend.Application.LLM;
using backend.Api.Helpers; 

namespace backend.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var app = CreateWebApplication(args);
        await app.RunAsync();
    }

    public static WebApplication CreateWebApplication(string[]? args)
    {
        var envPath = Path.Combine("..", "..", "..", ".env");
        if (File.Exists(envPath))
            Env.Load(envPath);

        var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        const string AllowFrontend = "_myAllowSpecificOrigins";

        // All config (including Shared section) now comes from appsettings.json
        builder.Services.Configure<SharedOptions>(builder.Configuration.GetSection(SharedOptions.SectionName));
        builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
        builder.Services.Configure<BackendOptions>(builder.Configuration.GetSection(BackendOptions.SectionName));
        builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));

        // Configure Kestrel server options
        var kestrelSection = builder.Configuration.GetSection("Kestrel");
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            var reqMin = kestrelSection.GetValue<int?>("RequestHeadersTimeoutMinutes") ?? 5;
            var keepAliveMin = kestrelSection.GetValue<int?>("KeepAliveTimeoutMinutes") ?? 5;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(reqMin);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(keepAliveMin);
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(AllowFrontend, policy =>
            {
                // Get cors origins from appsettings.json, shared section
                var origins = builder.Configuration.GetSection("Shared:CorsOrigins").Get<string[]>() ?? ["http://localhost:5173", "https://client.scalar.com"];
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            });
        });

        builder.Services.AddControllers();
        builder.Services.AddInfrastructure();
        builder.Services.AddDomain();
        builder.Services.AddApplication();

        builder.Services.AddOpenApi();
        builder.Services.AddAuthorization();

        // Periodically delete old generated files (TTL cleanup). Skip in test host.
        if (!builder.Environment.IsEnvironment("Testing"))
            builder.Services.AddHostedService<FileStorageTtlCleanupService>();

        var app = builder.Build();
        
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.MapGet("/", () => Results.Redirect("/scalar"));
        }

        if (!app.Environment.IsEnvironment("Testing"))
            app.UseHttpsRedirection();
        app.UseCors(AllowFrontend);
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}

