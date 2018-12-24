using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;
using Nest;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticSyncManager : IDocManager
    {
        private readonly ILogger<ElasticSyncManager> _logger;

        private readonly IAutoSyncElasticClient _client;
        //-- TODO: read list of collection/setting from config
        public List<string> CollectionsToSync => new List<string> { "ugc.review"};
        
        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }


        public ElasticSyncManager(ILogger<ElasticSyncManager> logger, IAutoSyncElasticClient client)
        {
            _logger = logger;
            _client = client;
        }

        public void ProcessUpsert(string collection, List<BsonDocument> documents)
        {
            _logger.LogInformation("Message received: {0} total", documents.Count);
            _client.BulkInsert(collection, documents);
        }

        public void ProcessDelete(string collection, HashSet<BsonValue> deleteIds)
        {

        }
    }
}