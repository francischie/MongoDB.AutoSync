using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Core.Services
{
    public class OplogService
    {
        private readonly IConfiguration _config;
        private readonly List<IDocManager> _docManagers = new List<IDocManager>(); 

        public OplogService(IConfiguration config)
        {
            _config = config;
        }

        public void Subscribe<T>()
        {
            //-- TODO: populate the list base on oplog message
            var list = new List<BsonDocument>();
            Task.WaitAll(_docManagers.Select(a => a.ProcessAsync(list)).ToArray());
        }
    }
}
