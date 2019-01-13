using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Core.Services
{
    public interface IDocManager
    {
        Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }
        void ProcessUpsert(string collection, List<BsonDocument> documents);
        void ProcessDelete(string collection, HashSet<BsonValue> deleteIds);
        IConfigMap ConfigMap { get; set; }
        SyncTracker GetSyncTracker(string collectionName);
        void RemoveOldSyncData(string indexName, long syncId);
        void UpdateSyncTracker(SyncTracker synctracker);
    }
}