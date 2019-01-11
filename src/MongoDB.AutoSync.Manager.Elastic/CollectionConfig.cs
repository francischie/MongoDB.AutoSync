using System.Collections.Generic;
using MongoDB.AutoSync.Core;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class CollectionConfig : ICollectionConfig
    {
        public string CollectionName { get; set; }
        public bool ExcludeAllByDefault { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string TargetName { get; set; }
        public string SyncIndexField { get; set; }

        public string GetTargetName()
        {
            return string.IsNullOrEmpty(TargetName) ? CollectionName : TargetName;
        }
    }
}
