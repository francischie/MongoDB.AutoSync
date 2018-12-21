using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Data.Client
{
    public class AutoSyncMongoClient : MongoClient
    {
        private readonly ILogger<AutoSyncMongoClient> _logger;
        private const string ConnectionStringName = "MongoDB";
        public AutoSyncMongoClient(IConfiguration config, ILogger<AutoSyncMongoClient> logger) : base(config.GetConnectionString(ConnectionStringName))
        {
            LogServerInfo(config.GetConnectionString(ConnectionStringName));
            _logger = logger;
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
