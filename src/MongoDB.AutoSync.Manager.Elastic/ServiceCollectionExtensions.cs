using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Services;
using Nest;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticSyncManager(this IServiceCollection services)
        {
            services.AddSingleton<IAutoSyncElasticClient, AutoSyncElasticClient>();
            return services;
        }
    }
}
