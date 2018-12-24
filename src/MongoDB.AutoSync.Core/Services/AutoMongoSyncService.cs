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
        private readonly BlockingCollection<BsonDocument> _documentLimiter = new BlockingCollection<BsonDocument>(2);
        private readonly ConcurrentQueue<BsonDocument> _queue = new ConcurrentQueue<BsonDocument>();
        
        // ReSharper disable once NotAccessedField.Local
        private Timer _queueVisitorTimer;

        public AutoMongoSyncService(IMongoClient client, ILogger<AutoMongoSyncService> logger)
        {
            _client = client;
            _logger = logger;
        }

        private void TimerCallback(object timerState)
        {
            if (!_queue.Any()) return;
            var limit = new List<BsonDocument>();

            while (_queue.TryDequeue(out var document) || limit.Count >= 100)
                limit.Add(document);

            var groupByCollection = limit.GroupBy(a => a["ns"].ToString(), a => a)
                .ToDictionary(a => a.Key, a => a.ToList());

            foreach (var g in groupByCollection)
            {
                var collectionName = g.Key.Split(".".ToCharArray());
                var collection = _client.GetDatabase(collectionName[0]).GetCollection<BsonDocument>(collectionName[1]);
                var builder = Builders<BsonDocument>.Filter;

                var upsertIds = g.Value.Where(doc => doc["op"] != "d").Select(doc => (doc["o"] ?? doc["o2"])["_id"]).ToHashSet();
                var deleteIds = g.Value.Where(doc => doc["op"] == "d").Select(doc => (doc["o"] ?? doc["o2"])["_id"]).ToHashSet();

                var filter = builder.In("_id", upsertIds);
                var query = collection.Find(filter);
                var list = query.ToList();

                if (!list.Any() && !deleteIds.Any()) continue; 

                Task.WaitAll(AutoSyncManager.Managers.Select(m => Task.Run(() =>
                {
                    if (list.Any()) m.ProcessUpsert(g.Key, list);
                    if (deleteIds.Any()) m.ProcessDelete(g.Key, deleteIds);
                })).ToArray());
            }
          
        }


        public Task StartAsync()
        {
            Task.Run(() => StartQueue());
            _queueVisitorTimer = new Timer(TimerCallback, null, 0, 500);
            return Task.Run(() => StartTailingOplog());
        }

        private void StartQueue()
        {
            foreach (var document in _documentLimiter.GetConsumingEnumerable())
                _queue.Enqueue(document);

          
        }


        private void StartTailingOplog()
        {
            try
            {
                var collectionNames = AutoSyncManager.Managers.SelectMany(a => a.CollectionsToSync).ToHashSet();

                var collection = _client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.In("ns", collectionNames);
                var options = new FindOptions<BsonDocument>
                {
                    CursorType = CursorType.TailableAwait
                };

                while (true)
                {
                    try
                    {
                        using (var cursor = collection.FindSync(filter, options))
                        {
                            foreach(var document in cursor.ToEnumerable())
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
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public static void Start()
        {

        }
    }
}
