using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticSyncManager : IDocManager
    {
        private readonly ILogger<ElasticSyncManager> _logger;
        private readonly IAutoSyncElasticClient _client;
        private readonly IElasticConfigMap _configMap;

        private readonly HashSet<string> _trackedCollection = new HashSet<string>();

        public IEnumerable<string> CollectionsToSync {
            get
            {
                return _configMap.GetCollectionConfig().Select(a => a.Key);
            }
        }

        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }


        public ElasticSyncManager(ILogger<ElasticSyncManager> logger, IAutoSyncElasticClient client, IElasticConfigMap configMap)
        {
            _logger = logger;
            _client = client;
            _configMap = configMap;
        }

        public void ProcessUpsert(string collection, List<BsonDocument> documents)
        {
            TryCreateIndexIfNotExist(collection, documents.First());
            _logger.LogInformation("Message received: {0} total", documents.Count);
            _client.BulkInsert(GeneratePayload(collection, documents));
        }

        public void ProcessDelete(string collection, HashSet<BsonValue> deleteIds)
        {

        }

        private void TryCreateIndexIfNotExist(string collectionName, BsonDocument document)
        {
            if (_trackedCollection.Contains(collectionName)) return;
            _trackedCollection.Add(collectionName);
            
            var config = _configMap.GetCollectionConfig()[collectionName];

            var indexName = config.GetTargetName(); 

            var dict =  CreateMappingDictionary(document.ToDictionary(), config);

            if (dict.Count == 0) return;

            var indexExist = _client.IsIndexExist(indexName);

            if (indexExist)
            {
                var index = new
                {
                    properties = dict
                };
                var body = JsonConvert.SerializeObject(index);
                _client.UpdateIndex(indexName, body);

            }
            else
            {
                var index = new
                {
                    mappings = new
                    {
                        doc = new
                        {
                            properties = dict
                        }
                    }
                };
                var body = JsonConvert.SerializeObject(index);
                _client.CreateIndex(indexName, body);
            }
        }


        public string GeneratePayload(string collectionName, List<BsonDocument> documents)
        {
            var json = new StringBuilder();
            var config = _configMap.GetCollectionConfig()[collectionName];

            var indexName = config.GetTargetName();

            foreach (var d in documents)
            {
                var dict = d.ToDictionary();
                var id = dict["_id"];

                RemoveUnmapProperties(dict, config);

                var update = new
                {
                    update = new
                    {
                        _index = indexName,
                        _type = "doc",
                        _id = id
                    }
                };

                var doc = new
                {
                    doc = dict,
                    doc_as_upsert = true
                };

                json.AppendLine(JsonConvert.SerializeObject(update));
                json.AppendLine(JsonConvert.SerializeObject(doc));
            }
            return json.ToString();
        }

        private void RemoveUnmapProperties(Dictionary<string, object> source, ElasticCollectionConfig config)
        {
            source.Remove("_id");
            var keys = source.Keys.ToList();
            if (config?.ExcludeAllByDefault == false) return; 

            foreach (var k in keys)
                if (config?.Properties?.ContainsKey(k) == false)
                    source.Remove(k);
        }

        private Dictionary<string, object> CreateMappingDictionary(Dictionary<string, object> source, ElasticCollectionConfig config)
        {
            var list = new Dictionary<string, object>();

            var keys = config == null || config.ExcludeAllByDefault == false  ? source.Keys : config.Properties.Keys;

            foreach (var k in keys)
            {
                if (k == "_id") continue;

                var customMap = config?.Properties == null || !config.Properties.ContainsKey(k) 
                            ? null : config.Properties[k];

                if (customMap != null && !(customMap is string))
                {
                    list.Add(k, config.Properties[k]);
                    continue;
                }

                if (source.ContainsKey(k) && source[k] is Guid)
                    list.Add(k, new { type = "keyword"});

            }
            return list;
        }

    }
}