using System.Collections.Generic;

namespace MongoDB.AutoSync.Core
{
    public interface ICollectionConfig
    {
        string Name { get; set; }
        bool ExcludeAllByDefault { get; set; }
        Dictionary<string, object> Properties { get; set; }
    }
}
