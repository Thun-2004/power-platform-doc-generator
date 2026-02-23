using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Application

{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
        {
            return services;
        }
    }
}