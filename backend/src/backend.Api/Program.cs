using Scalar.AspNetCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using backend.Data;
using backend.Domain;
using backend.Infrastructure;
using backend.Application;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using backend.Application.Interfaces;
using backend.Application.Services;
using backend.Infrastructure.Storage;

namespace backend.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var AllowFrontend = "_myAllowSpecificOrigins"; 

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: AllowFrontend,
                            policy  =>
                            {
                                policy.WithOrigins(
                                    "http://localhost:5173", 
                                    "https://client.scalar.com"
                                ).AllowAnyHeader()
                                .AllowAnyMethod();
                            });
        });
        
        // Add services
        builder.Services.AddControllers();
        builder.Services.AddInfrastructure();
        builder.Services.AddDomain();
        builder.Services.AddApplication();

        //in memory storage
        builder.Services.AddSingleton<IJobStore, JobStore>();

        //DI
        builder.Services.AddScoped<FileProcessing>(); 
        builder.Services.AddScoped<IUploadService, UploadService>();
        builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
        builder.Services.AddScoped<IJobStatusService, JobStatusService>();

        //scalar UI
        builder.Services.AddOpenApi();

        builder.Services.AddAuthorization(); 

        //TODO: set global request time outs
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
        });

        var app = builder.Build();

        //Authentication -> later
        // builder.Services.AddDbContext<AppDbContext>(options =>
        // {
        //     options.UseInMemoryDatabase("AuthDb"); 
        // }
        // );
        // app.MapIdentityApi<IdentityUser>(); 
        

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.MapGet("/", () => Results.Redirect("/scalar")); 
        }

        app.UseAuthentication();
        app.UseHttpsRedirection();
        app.UseCors(AllowFrontend);
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}


