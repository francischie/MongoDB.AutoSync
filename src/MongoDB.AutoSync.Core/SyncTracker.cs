using System;

namespace MongoDB.AutoSync.Core
{
    public class SyncTracker
    {
        public DateTime LastSyncDate { get; set; }
        public string CollectionName { get; set; }
        public object LastReferenceId { get; set; }
        public long SyncVersionId { get; set; }
    }
}
