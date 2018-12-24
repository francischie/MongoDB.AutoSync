using Microsoft.Extensions.DependencyInjection;

namespace MongoDB.AutoSync.Manager.Elastic.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElasticSyncManager(this IServiceCollection services)
        {
            services.AddSingleton<IAutoSyncElasticClient, AutoSyncElasticClient>();
            services.AddSingleton<IElasticConfigMap, ElasticConfigMap>();
            return services;
        }
    }
}
