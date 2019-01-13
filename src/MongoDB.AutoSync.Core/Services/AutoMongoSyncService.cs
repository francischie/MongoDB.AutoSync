using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Services
{
    public class AutoMongoSyncService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<AutoMongoSyncService> _logger;
        private readonly BlockingCollection<BsonDocument> _documentLimiter = new BlockingCollection<BsonDocument>(BufferSize);
        const int BufferSize = 1000;
        private CancellationToken _cancellationToken;

        public AutoMongoSyncService(IMongoClient client, ILogger<AutoMongoSyncService> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Start auto sync on non-blocking state and return CancellationTokenSource
        /// </summary>
        /// <returns></returns>
        public CancellationTokenSource StartAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            StartAsync(token);
            return cancellationTokenSource;
        }

        /// <summary>
        /// Start auto sync on non-blocking state
        /// </summary>
        /// <returns></returns>
        public void StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            StartWithLogging(StartDumping);
            Task.Run(() => StartWithLogging(ConsumeQueue), _cancellationToken);
            Task.Run(() => StartWithLogging(ProduceQueue), _cancellationToken);
        }
        
        private void StartDumping()
        {
            _logger.LogInformation("Starting dump for all sync manager");
            foreach (var m in AutoSyncManagers.Managers)
                foreach (var c in m.ConfigMap.CollectionConfigs.Values)
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
                    document["syncVersionId"] = synctracker.SyncVersionId;
                    documentLimiter.Add(document);
                    if (documentLimiter.Count < 1000) continue;
                    m.ProcessUpsert(c.CollectionName, documentLimiter);

                    synctracker.LastSyncDate = DateTime.UtcNow;
                    synctracker.LastReferenceId = documentLimiter.Last()[c.SyncIndexField];
                    m.UpdateSyncTracker(synctracker);

                    documentLimiter.Clear();
                    _logger.LogInformation("Total number of record processed: {0}", counter);
                }

                if (documentLimiter.Any())
                {
                    m.ProcessUpsert(c.CollectionName, documentLimiter);
                    documentLimiter.Clear();
                }
                _logger.LogInformation("Dump completed for collection {0}! Total records: {1}", c.CollectionName, counter);
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
            while (_cancellationToken.IsCancellationRequested == false)
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

                    Task.WaitAll(AutoSyncManagers.Managers.Select(m => Task.Run(() =>
                    {
                        _logger.LogInformation("Received message from oplog. {0}", list.Count);
                        if (list.Any())
                        {
                            m.ProcessUpsert(g.Key, list);
                            //-- TODO: evaluate if we can also update synctracker
                        }
                        if (deleteIds.Any()) m.ProcessDelete(g.Key, deleteIds);

                    }, _cancellationToken)).ToArray());
                }
            }

            _logger.LogInformation("AutoMongoSync queue consumer terminated!");
        }

        /// <summary>
        /// Produce queue continuously and ignore error.  
        /// </summary>
        private void ProduceQueue()
        {
            _logger.LogInformation("AutoMongoSync queue producer started!");
            while (_cancellationToken.IsCancellationRequested == false)
            {
             
                var collectionNames = AutoSyncManagers.Managers.SelectMany(a => a.ConfigMap.CollectionConfigs.Values.Select(b => b.CollectionName)).ToHashSet();

                var collection = _client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
                var builder = Builders<BsonDocument>.Filter;
                var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var filter = builder.Gte("ts", new BsonTimestamp((int)seconds, 1)) & builder.In("ns", collectionNames);
                var options = new FindOptions<BsonDocument>
                {
                    CursorType = CursorType.TailableAwait,
                    OplogReplay = true
                };
                try
                {
                    using (var cursor = collection.FindSync(filter, options))
                    {
                        foreach (var document in cursor.ToEnumerable(_cancellationToken))
                        {
                            _documentLimiter.Add(document, _cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("AutoMongoSync queue producer terminated.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                Thread.Sleep(1000);
            }

        }



    }


}
