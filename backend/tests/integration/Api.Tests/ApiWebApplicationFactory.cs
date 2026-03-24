using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.IntegrationTests;

/// <summary>In-process API host for integration tests (TestServer).</summary>
public class ApiWebApplicationFactory : WebApplicationFactory<backend.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
