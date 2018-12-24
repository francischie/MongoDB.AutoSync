using System;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public interface IAutoSyncElasticClient
    {
        void BulkInsert(string payload);
        bool IsIndexExist(string collectionName);
        void CreateIndex(string collectionName, string body);
        void UpdateIndex(string collectionName, string body);
    }
    public class AutoSyncElasticClient : IAutoSyncElasticClient
    {
        private readonly IElasticClient _client;


        public AutoSyncElasticClient(IConfiguration config)
        {
            _client = new ElasticClient(CreateConnection(config));
        }

        private ConnectionSettings CreateConnection(IConfiguration config)
        {
            var uris = config.GetConnectionString("Elastic").Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(a => new Uri(a));
            var pool = new StaticConnectionPool(uris);
            var connection = new ConnectionSettings(pool, (builtin, settings) => new JsonNetSerializer(
                builtin, settings, () => new JsonSerializerSettings {  DefaultValueHandling = DefaultValueHandling.Populate }));
            return connection;
        }

        public void BulkInsert(string payload)
        {
            _client.LowLevel.Bulk<StringResponse>(payload);
        }

        public bool IsIndexExist(string collectionName)
        {
            var response = _client.LowLevel.IndicesExists<StringResponse>(collectionName);
            return response.HttpStatusCode == 200;
        }

        public void CreateIndex(string collectionName, string body)
        {
            _client.LowLevel.IndicesCreate<StringResponse>(collectionName, body);
        }

        public void UpdateIndex(string collectionName, string body)
        {
            _client.LowLevel.IndicesPutMapping<StringResponse>(collectionName, "doc", body);

        }
    }


  
}