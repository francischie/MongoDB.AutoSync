using System.Collections.Generic;

namespace MongoDB.AutoSync.Core
{
    public interface IConfigMap
    {
        Dictionary<string, CollectionConfig> CollectionConfigs { get; }
    }
}