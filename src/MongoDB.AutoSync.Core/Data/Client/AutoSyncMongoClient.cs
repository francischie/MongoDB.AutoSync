using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Data.Client
{
    public class AutoSyncMongoClient : MongoClient
    {
        private readonly ILogger<AutoSyncMongoClient> _logger;
        public AutoSyncMongoClient(IConfiguration config, ILogger<AutoSyncMongoClient> logger, string connectionName = "MongoDB") : base(config.GetConnectionString(connectionName))
        {
            _logger = logger;
            LogServerInfo(config.GetConnectionString(connectionName));
            MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;
        }

        private void LogServerInfo(string connectionString)
        {
            try
            {
                var hostInfo = connectionString.Substring(connectionString.IndexOf("@", StringComparison.Ordinal) + 1);
                _logger.LogInformation("AutoSyncMongoServer: {0}", hostInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error connecting parsing connection string!");
            }
        }
    }
}
