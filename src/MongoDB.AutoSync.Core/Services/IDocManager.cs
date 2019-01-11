using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Core.Services
{
    public interface IDocManager
    {
        IEnumerable<string> CollectionsToSync { get; }
        Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }
        void ProcessUpsert(string collection, List<BsonDocument> documents, bool dump = false);
        void ProcessDelete(string collection, HashSet<BsonValue> deleteIds);
        IEnumerable<ICollectionConfig> CollectionConfigs { get; }
        SyncTracker GetSyncTracker(string collectionName);
        void RemoveOldSyncData(string indexName, long syncId);
    }
}