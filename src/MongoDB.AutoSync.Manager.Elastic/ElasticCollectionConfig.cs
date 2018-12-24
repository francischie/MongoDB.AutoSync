using System.Collections.Generic;
using MongoDB.AutoSync.Core;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticCollectionConfig : ICollectionConfig
    {
        public string CollectionName { get; set; }
        public bool ExcludeAllByDefault { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string IndexName { get; set; }

        public string GetIndexName()
        {
            return string.IsNullOrEmpty(IndexName) ? CollectionName : IndexName;
        }
    }
}
