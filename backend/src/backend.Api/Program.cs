using Scalar.AspNetCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using backend.Data;
using backend.Domain;
using backend.Infrastructure;

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace backend.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var AllowFrontend = "_myAllowSpecificOrigins"; //cors rule's name

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
        
        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddInfrastructures();
        //FIX: in memory cache for testing
        builder.Services.AddSingleton<IUploadStore, UploadStore>();
        // builder.Services.AddApplication();

        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase("AuthDb"); 
        }
        );

        builder.Services.AddAuthorization(); 

        //setup table (in memory)
        builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<AppDbContext>(); 

        //TODO: set global request time outs
        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
        });

        var app = builder.Build();

        //map 
        app.MapIdentityApi<IdentityUser>(); 

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();

            // Automatically redirect to Scalar documentation (only in dev)
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


