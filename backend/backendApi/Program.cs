using Scalar.AspNetCore;

using Microsoft.AspNetCore.Identity;
// using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using backendApi.Data;


namespace backendApi; 

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
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
        app.UseAuthorization();
        app.MapControllers();
        app.UseCors("AllowAll"); 
        app.Run();
    }
}
