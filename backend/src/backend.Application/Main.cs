// Summary: Registers core application-layer services (file processing, upload, job status, and job output) into the DI container.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using backend.Application.LLM;
using backend.Application.Interfaces;
using backend.Application.Services;

namespace backend.Application

{
    // Summary: Provides extension methods to register application services.
    public static class ServiceCollectionExtensions
    {
        // Summary: Adds application-layer services (LLM file processing, upload, status, and output) to the provided service collection.
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