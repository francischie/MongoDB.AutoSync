using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Core.Services
{
    public interface IDocManager
    {
        List<string> CollectionsToSync { get; set; }
        Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }
        Task ProcessAsync(List<BsonDocument> documents);
        void Process();
        

    }
}