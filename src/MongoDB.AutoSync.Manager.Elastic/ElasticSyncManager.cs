using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core;
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
        private readonly Dictionary<string, SyncTracker> _syncTrackers;
            
        const string SyncTrackerIndexName = "synctracker"; 
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
            _syncTrackers = new Dictionary<string, SyncTracker>();
        }

        public void ProcessUpsert(string collection, List<BsonDocument> documents, bool dump = false)
        {
            var synctracker = GetSyncTracker(collection);
            if (synctracker == null)
            {
                _logger.LogError("Error getting synctracker");
                return;
            }

            CreateIndexIfNotExist(collection, documents.First());
            var bulkUpsert = GenerateBulkUpsert(collection, documents, synctracker);
            if (dump)
                bulkUpsert += GenerateSingleUpsert(SyncTrackerIndexName, collection, synctracker);

            _client.Bulk(bulkUpsert);
        }
        public void ProcessDelete(string collection, HashSet<BsonValue> deleteIds)
        {
        }

        public void RemoveOldSyncData(string indexName, long syncId)
        {
            _client.BulkDelete(indexName, syncId);
        }
        public IEnumerable<ICollectionConfig> CollectionConfigs => _configMap.GetCollectionConfig().Values;
   

        private void CreateIndexIfNotExist(string collectionName, BsonDocument document)
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
        
        private string GenerateBulkUpsert(string collectionName, List<BsonDocument> documents, SyncTracker synctracker)
        {
            var bulkPayload = new StringBuilder();
            var config = _configMap.GetCollectionConfig()[collectionName];
            var indexName = config.GetTargetName();

            foreach (var d in documents)
            {
                var dict = d.ToDictionary();
                var id = dict["_id"];
                dict.Add("syncVersionId", synctracker.SyncVersionId);
                RemoveUnmapProperties(dict, config);
                var single = GenerateSingleUpsert(indexName, id, dict);
                bulkPayload.AppendLine(single);
                synctracker.LastSyncDate = DateTime.UtcNow;
                if (dict.ContainsKey(config.SyncIndexField))
                    synctracker.LastReferenceId = dict[config.SyncIndexField];
            }
            return bulkPayload.ToString();
        }

        private void RemoveUnmapProperties(Dictionary<string, object> source, CollectionConfig config)
        {
            source.Remove("_id");
            var keys = source.Keys.ToList();
            if (config?.ExcludeAllByDefault == false) return; 

            foreach (var k in keys)
                if (config?.Properties?.ContainsKey(k) == false)
                    source.Remove(k);
        }

        private Dictionary<string, object> CreateMappingDictionary(Dictionary<string, object> source, CollectionConfig config)
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

        public SyncTracker GetSyncTracker(string collectionName)
        {
            _syncTrackers.TryGetValue(collectionName, out var syncTracker);

            if (syncTracker != null) return syncTracker;

            var response = _client.Get<SyncTracker>(SyncTrackerIndexName, collectionName);

            if (response.Found == false || response.ServerError?.Status == (int) HttpStatusCode.NotFound)
            {
                syncTracker = new SyncTracker
                {
                    CollectionName = collectionName,
                    SyncVersionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                var upsert = GenerateSingleUpsert(SyncTrackerIndexName, collectionName, _syncTrackers);
                _client.Bulk(upsert);
            }
            else
            {
                syncTracker = response.Source;
            }

            _syncTrackers.Add(collectionName, syncTracker);
            return syncTracker;
        }

        private string GenerateSingleUpsert<T>(string index, object id , T model)
        {
            var action = new { update = new { _index = index, _type = "doc", _id = id } };
            var doc = new { doc = model, doc_as_upsert = true };

            var json = new StringBuilder();
            json.AppendLine(JsonConvert.SerializeObject(action));
            json.AppendLine(JsonConvert.SerializeObject(doc));

            return json.ToString();
        }

    }
}