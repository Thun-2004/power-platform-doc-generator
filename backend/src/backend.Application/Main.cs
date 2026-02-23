using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Application.LLM;
using backend.Application.Interfaces;
using backend.Application.Services;

namespace backend.Application

{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddScoped<FileProcessing>(); 
            services.AddScoped<IUploadService, UploadService>();
            services.AddScoped<IJobStatusService, JobStatusService>();
            services.AddScoped<IJobOutputService, JobOutputService>();
            return services;
        }
    }
}