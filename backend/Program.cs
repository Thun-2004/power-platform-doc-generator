using Scalar.AspNetCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using backend.Data;
using backend.Infrastructure;

namespace backend; 

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; //cors rule's name

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                            policy  =>
                            {
                                policy.WithOrigins("http://example.com",
                                                    "http://www.contoso.com");
                            });
        });
        
        //NOTE: testing only
        // var uploadRoot = Path.Combine(
        //     Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
        //     "uploads-test"
        // ); 
        // Directory.CreateDirectory(uploadRoot); 

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
        app.UseCors(MyAllowSpecificOrigins);
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
