using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticSyncManager : IDocManager
    {
        public List<string> CollectionsToSync { get; set; }
        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }


        public ElasticSyncManager()
        {
            
        }

        public Task ProcessAsync(List<BsonDocument> documents)
        {
            if (OnDocumentReceivedAsync != null)
                return OnDocumentReceivedAsync(documents);
           
            //-- TODO: by convension
            return null; 
        }

        public void Process()
        {

        }

    }
}