using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core;

namespace MongoDB.AutoSync.Manager.Elastic.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticSyncManager(this IServiceCollection services)
        {
            services.AddSingleton<IAutoSyncElasticClient, AutoSyncElasticClient>();
            services.AddSingleton<IConfigMap, ElasticConfigMap>();
            return services;
        }
    }
}
