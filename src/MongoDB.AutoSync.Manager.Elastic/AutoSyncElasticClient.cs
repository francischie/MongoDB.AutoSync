using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public interface IAutoSyncElasticClient
    {
        void BulkInsert(string collectionName, List<BsonDocument> documents);
    }
    public class AutoSyncElasticClient : IAutoSyncElasticClient
    {
        private readonly IElasticClient _client;

        private HashSet<string> TrackCollection = new HashSet<string>();

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

        public void BulkInsert(string collectionName, List<BsonDocument> documents)
        {
            if (!TrackCollection.Contains(collectionName))
            {
                InitializeMapping(collectionName, documents.First());
                TrackCollection.Add(collectionName);
            }

            var json = new StringBuilder();
            
            foreach (var d in documents)
            {
                var id = ((Guid)d["_id"]).ToString();
                var update = new
                {
                    update = new
                    {
                        _index = collectionName,
                        _type = "doc",
                        _id = id
                    },
                };

                var content = d.ToDictionary();
                content.Remove("_id");
                var doc = new
                {
                    doc = content,
                    doc_as_upsert = true
                };

                json.AppendLine(JsonConvert.SerializeObject(update));
                json.AppendLine(JsonConvert.SerializeObject(doc));
            }
            var postBody = json.ToString();
            _client.LowLevel.Bulk<StringResponse>(postBody);
        }

        private void InitializeMapping(string collectionName, BsonDocument document)
        {
            var response = _client.LowLevel.IndicesExists<StringResponse>(collectionName);
            if (response.Success) return;

            var content = document.ToDictionary()
                .Where(a => a.Value is Guid && a.Key != "_id")
                .ToDictionary(a => a.Key, a => new { type = "keyword"});
            var index = new
            {
                mappings = new
                {
                    doc = new {
                        properties = content
                    }
                }
            };
            var body = JsonConvert.SerializeObject(index);
            _client.LowLevel.IndicesCreate<StringResponse>(collectionName, body);
        }
    }


  
}