using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Infrastructure

{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructures(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // Register infrastructure services here, e.g.:
            // services.AddScoped<IMyRepo, MyRepo>();
            // If infra configures DB contexts, add them here (or keep DB registration in the Web project).
            
            
            return services;
        }
    }
}

