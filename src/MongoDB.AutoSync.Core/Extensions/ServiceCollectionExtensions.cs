using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Data.Client;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoAutoSync(this IServiceCollection services)
        {
            MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;

            BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(BsonType.String));
            

            services.AddSingleton<IAutoMongoSyncConfiguration, AutoMongoSyncConfiguration>();

            if (services.IsServiceRegistered<IMongoClient>()) return services;
            services.AddSingleton<IMongoClient, AutoSyncMongoClient>();

            //-- TODO: auto discover all instance of class derived from IDocManager and register
            
            return services;
        }
        
        public static bool IsServiceRegistered<TService>(this IServiceCollection services)
        {
            return services.Any(a => a.ServiceType == typeof(TService));
        }
    }
}
