using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticSyncManager : IDocManager
    {
        private readonly ILogger<ElasticSyncManager> _logger;
        public List<string> CollectionsToSync { get; set; }

        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }


        public ElasticSyncManager(ILogger<ElasticSyncManager> logger)
        {
            _logger = logger;
        }

        public void ProcessUpsert(string collection, List<BsonDocument> documents)
        {
            _logger.LogInformation("Message received: {0} total", documents.Count);
 }

        public Task DeleteAsync(List<BsonValue> ids)
        {

            throw new NotImplementedException();
        }


    }
}