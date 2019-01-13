using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.AutoSync.Core;
using Newtonsoft.Json;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticConfigMap : IConfigMap
    {
        private Dictionary<string, CollectionConfig> _collectionConfigs;

        public Dictionary<string, CollectionConfig> CollectionConfigs
        {
            get
            {
                if (_collectionConfigs != null) return _collectionConfigs;

                var basePath = GetBasePath();
                var path = Path.Combine(basePath, "ElasticConfingMap.json");
                var content = File.ReadAllText(path);
                _collectionConfigs = JsonConvert.DeserializeObject<List<CollectionConfig>>(content)
                    .ToDictionary(a => a.CollectionName, a => a);
                return _collectionConfigs;
            }
        }

        private string GetBasePath()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }
    }
}
