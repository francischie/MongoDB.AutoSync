using System.Collections.Generic;

namespace MongoDB.AutoSync.Core
{
    public interface ICollectionConfig
    {
        string CollectionName { get; set; }
        bool ExcludeAllByDefault { get; set; }
        Dictionary<string, object> Properties { get; set; }
        string TargetName { get; set; }
        string SyncIndexField { get; set; }
        string GetTargetName();
    }
}
