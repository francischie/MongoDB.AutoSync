using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public interface IElasticConfigMap
    {
        Dictionary<string, CollectionConfig> GetCollectionConfig();
    }

    public class ElasticConfigMap : IElasticConfigMap
    {
        private Dictionary<string, CollectionConfig> _collectionConfigs;


        public ElasticConfigMap()
        {
        }

        public Dictionary<string, CollectionConfig> GetCollectionConfig()
        {
            if (_collectionConfigs != null) return _collectionConfigs;

            var basePath = GetBasePath();
            var path = Path.Combine(basePath, "elasticconfigmap.json");
            var content = File.ReadAllText(path);
            _collectionConfigs = JsonConvert.DeserializeObject<List<CollectionConfig>>(content)
                .ToDictionary(a => a.CollectionName, a => a);
            return _collectionConfigs;
        }

        private string GetBasePath()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }
    }
}
