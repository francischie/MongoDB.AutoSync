using System.Collections.Generic;
using MongoDB.AutoSync.Core;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticCollectionConfig : ICollectionConfig
    {
        public string Name { get; set; }
        public bool ExcludeAllByDefault { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}
