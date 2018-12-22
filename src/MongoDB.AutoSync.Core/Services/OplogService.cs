using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Services
{
    public class OplogService
    {
        private readonly IConfiguration _config;
        private readonly IMongoClient _client;
        private readonly ILogger<OplogService> _logger;

        public OplogService(IConfiguration config, IMongoClient client, ILogger<OplogService> logger)
        {
            _config = config;
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                Thread.Sleep(3000);
                var collectionNames = AutoSyncManager.Managers.SelectMany(a => a.CollectionsToSync).ToList();

                var collection = _client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.In("ns", collectionNames);
                var options = new FindOptions<BsonDocument>
                {
                    CursorType = CursorType.TailableAwait
                };

                while (true)
                {
                    using (var cursor = await collection.FindAsync(filter, options, cancellationToken))
                    {
                        await cursor.ForEachAsync(document =>
                        {
                            _logger.LogInformation("recevied message...");
                        }, cancellationToken: cancellationToken);
                    }
                    Thread.Sleep(1000);
                }
            }, cancellationToken);
        }
    }
}
