using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class MsSqlSyncManager : IDocManager
    {

        private readonly ILogger<MsSqlSyncManager> _logger;

        public MsSqlSyncManager(ILogger<MsSqlSyncManager> logger)
        {
            _logger = logger;
        }

        public List<string> CollectionsToSync { get; set; }
        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }
        public void Upsert(BsonValue id, MongoOperation action)
        {
            _logger.LogInformation("Message received from MsSql");
            Thread.Sleep(5000);

        }

        public Task DeleteAsync(List<BsonValue> ids)
        {
            throw new NotImplementedException();
        }
    }
}