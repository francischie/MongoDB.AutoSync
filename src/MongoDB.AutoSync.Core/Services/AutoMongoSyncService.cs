using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Services
{
    public class AutoMongoSyncService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<AutoMongoSyncService> _logger;
        private readonly BlockingCollection<BsonDocument> _documentLimiter = new BlockingCollection<BsonDocument>(BufferSize);
        const int BufferSize = 1000;

        public AutoMongoSyncService(IMongoClient client, ILogger<AutoMongoSyncService> logger)
        {
            _client = client;
            _logger = logger;
        }


        /// <summary>
        /// Start auto sync on non-blocking state and 
        /// </summary>
        /// <returns></returns>
        public void StartAutoSync()
        {
            Task.Run(() => StartWithLogging(StartDumping)).ContinueWith(task =>
            {
                Task.Run(() => StartWithLogging(ConsumeQueue));
                Task.Run(() => StartWithLogging(ProduceQueue));
            });
        }

        private void StartDumping()
        {
            _logger.LogInformation("Starting dump for all sync manager");
            foreach (var m in AutoSyncManager.Managers)
                foreach (var c in m.CollectionConfigs)
                    GetAllRecordsForTheCollection(m, c);
        }

        private void GetAllRecordsForTheCollection(IDocManager m, ICollectionConfig c)
        {
            _logger.LogInformation("Performing dump from {0} to {1}", c.CollectionName, c.TargetName);
            var builder = Builders<BsonDocument>.Filter;
            var synctracker = m.GetSyncTracker(c.CollectionName);
            var filter = synctracker.LastReferenceId == null
                ? builder.Empty
                : builder.Gt(c.SyncIndexField, synctracker.LastReferenceId);
            var dbCollectName = c.CollectionName.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var collection = _client.GetDatabase(dbCollectName[0]).GetCollection<BsonDocument>(dbCollectName[1]);
            var documentLimiter = new List<BsonDocument>();
            var sorter = Builders<BsonDocument>.Sort.Ascending(c.SyncIndexField);
            var options = new FindOptions<BsonDocument> { NoCursorTimeout = true, Sort = sorter };
            var counter = 0;
            using (var cursor = collection.FindSync(filter, options))
            {
                foreach (var document in cursor.ToEnumerable())
                {
                    counter++;
                    documentLimiter.Add(document);
                    if (documentLimiter.Count < 1000) continue;
                    m.ProcessUpsert(c.CollectionName, documentLimiter, true);
                    documentLimiter.Clear();
                    _logger.LogInformation("Total number of record processed: {0}", counter);
                }

                if (documentLimiter.Any())
                {
                    m.ProcessUpsert(c.CollectionName, documentLimiter, true);
                    documentLimiter.Clear();
                }
                _logger.LogInformation("Dump completed for collection {0}!", c.CollectionName);
                m.RemoveOldSyncData(c.TargetName, synctracker.SyncVersionId);
            }

        }

        private void StartWithLogging(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Process queue continuously and ignore error. 
        /// </summary>
        private void ConsumeQueue()
        {
            while (true)
            {
                var limit = new List<BsonDocument>();

                while (_documentLimiter.TryTake(out var document) && limit.Count < BufferSize)
                    limit.Add(document);

                if (limit.Any() == false)
                {
                    Thread.Sleep(500);
                    continue;
                }

                var groupByCollection = limit.GroupBy(a => a["ns"].ToString(), a => a)
                    .ToDictionary(a => a.Key, a => a.ToList());

                var builder = Builders<BsonDocument>.Filter;

                foreach (var g in groupByCollection)
                {
                    var collectionName = g.Key.Split(".".ToCharArray());
                    var collection = _client.GetDatabase(collectionName[0]).GetCollection<BsonDocument>(collectionName[1]);

                    var upsertIds = g.Value.Where(doc => doc["op"] != "d").Select(doc => (doc["o"] ?? doc["o2"])["_id"]).ToHashSet();
                    var deleteIds = g.Value.Where(doc => doc["op"] == "d").Select(doc => (doc["o"] ?? doc["o2"])["_id"]).ToHashSet();

                    var filter = builder.In("_id", upsertIds);
                    var query = collection.FindSync(filter);
                    var list = query.ToList();

                    if (!list.Any() && !deleteIds.Any()) continue;

                    Task.WaitAll(AutoSyncManager.Managers.Select(m => Task.Run(() =>
                    {
                        if (list.Any()) m.ProcessUpsert(g.Key, list);
                        if (deleteIds.Any()) m.ProcessDelete(g.Key, deleteIds);
                    })).ToArray());
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        /// <summary>
        /// Produce queue continuously and ignore error.  
        /// </summary>
        private void ProduceQueue()
        {
            _logger.LogInformation("Start listening to opLog...");
            while (true)
            {
                var collectionNames = AutoSyncManager.Managers.SelectMany(a => a.CollectionsToSync).ToHashSet();

                var collection = _client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
                var builder = Builders<BsonDocument>.Filter;
                var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var filter = builder.Gte("ts", new BsonTimestamp((int)seconds, 1))
                    & builder.In("ns", collectionNames);

                var test = collection.Find(filter);

                var options = new FindOptions<BsonDocument>
                {
                    CursorType = CursorType.TailableAwait,
                    OplogReplay = true
                };
                try
                {
                    using (var cursor = collection.FindSync(filter, options))
                    {
                        foreach (var document in cursor.ToEnumerable())
                        {
                            _documentLimiter.Add(document);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                Thread.Sleep(1000);
            }
            // ReSharper disable once FunctionNeverReturns

        }



    }


}
