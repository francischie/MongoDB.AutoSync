using System.Collections.Generic;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Driver;

namespace MongoDB.AutoSync.Core.Extensions
{
    public class AutoSyncOptions
    {
        public string ConnectionStringName => "MongoDB";
        public readonly List<IDocManager> Managers  = new List<IDocManager>();
    }
}