using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Infrastructure.Storages;
using backend.Application.Interfaces; 

namespace backend.Infrastructure

{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddSingleton<IJobStore, JobStore>();
            return services;
        }
    }
}

