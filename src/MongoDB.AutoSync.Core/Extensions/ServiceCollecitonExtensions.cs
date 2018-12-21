using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Data.Client;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Extensions
{
    public static class ServiceCollecitonExtensions
    {
        public static IServiceCollection AddMongoElasticSync(this IServiceCollection services)
        {
            return services.AddSingleton<IMongoClient, AutoSyncMongoClient>();
        }
    }
}
