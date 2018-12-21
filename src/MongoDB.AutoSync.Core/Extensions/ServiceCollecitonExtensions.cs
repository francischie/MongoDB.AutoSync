using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Data.Client;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Extensions
{


    public static class ServiceCollecitonExtensions
    {
        public static IServiceCollection AddMongoElasticSync(this IServiceCollection services, Action<AutoSyncOptions> setupAction)
        {
            var defaultOption = new AutoSyncOptions();
            setupAction(defaultOption);

            if (services.IsServiceRegistered<IMongoClient>()) return services;
            
            return string.IsNullOrEmpty(defaultOption.ConnectionStringName) 
                ? services.AddSingleton<IMongoClient, AutoSyncMongoClient>()
                : services.AddSingleton(provider => new AutoSyncMongoClient(provider.GetService<IConfiguration>(), provider.GetService<ILogger<AutoSyncMongoClient>>(), defaultOption.ConnectionStringName));
        }
        
        public static bool IsServiceRegistered<TService>(this IServiceCollection services)
        {
            return services.Any(a => a.ServiceType == typeof(TService) == false);
        }
    }
}
