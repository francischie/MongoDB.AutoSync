using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public interface IElasticConfigMap
    {
        Dictionary<string, ElasticCollectionConfig> GetCollectionConfig();
    }

    public class ElasticConfigMap : IElasticConfigMap
    {
        private Dictionary<string, ElasticCollectionConfig> _collectionConfigs;

        private readonly IHostingEnvironment _environment;

        public ElasticConfigMap(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        public Dictionary<string, ElasticCollectionConfig> GetCollectionConfig()
        {
            if (_collectionConfigs != null) return _collectionConfigs;

            var path = Path.Combine(_environment.ContentRootPath, "elasticconfigmap.json");
            var content = File.ReadAllText(path);
            _collectionConfigs = JsonConvert.DeserializeObject<List<ElasticCollectionConfig>>(content)
                .ToDictionary(a => a.CollectionName, a => a);
            return _collectionConfigs;
        }
    }
}
