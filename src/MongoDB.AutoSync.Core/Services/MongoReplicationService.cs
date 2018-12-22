using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Services
{
    public enum MongoOperation
    {
        Upsert,
        Delete
    }

    public class MongoReplicationService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<MongoReplicationService> _logger;

        public MongoReplicationService(IMongoClient client, ILogger<MongoReplicationService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
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
                            using (var cursor = await collection.FindAsync(filter, options, cancellationToken))
                            {
                                await cursor.ForEachAsync(document =>
                                {

                                    var id = (document["o"] ?? document["o2"])["_id"];
                                    var action = document["op"] == "d" ? MongoOperation.Delete : MongoOperation.Upsert;

                                    var tasks = new List<Task>();
                                   
                                    AutoSyncManager.Managers.ForEach(m =>
                                    {
                                        tasks.Add(Task.Run(() => { m.Upsert(id, action); }, cancellationToken));
                                    });

                                    Task.WaitAll(tasks.ToArray());

                                }, cancellationToken);
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
               
                // ReSharper disable once FunctionNeverReturns
            }, cancellationToken);
        }

    }
}
