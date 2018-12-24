using System;
using System.Collections.Generic;

namespace MongoDB.AutoSync.Core.Services
{
    public interface IAutoMongoSyncConfiguration
    {

    }

    public class AutoMongoSyncConfiguration : IAutoMongoSyncConfiguration
    {
        public List<IDocManager> Managers { get; set; }

        public AutoMongoSyncConfiguration(Action<List<IDocManager>> configure)
        {
            configure.Invoke(Managers);
        }

    }
}