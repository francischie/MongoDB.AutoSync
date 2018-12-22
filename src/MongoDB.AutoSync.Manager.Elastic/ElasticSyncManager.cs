﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.AutoSync.Core.Services;
using MongoDB.Bson;

namespace MongoDB.AutoSync.Manager.Elastic
{
    public class ElasticSyncManager : IDocManager
    {
        private readonly ILogger<ElasticSyncManager> _logger;
        public List<string> CollectionsToSync { get; set; }

        // Event to allow customization before commiting 
        public Func<List<BsonDocument>, Task> OnDocumentReceivedAsync { get; set; }


        public ElasticSyncManager(ILogger<ElasticSyncManager> logger)
        {
            _logger = logger;
        }

        public void Upsert(BsonValue id, MongoOperation action)
        {
            _logger.LogInformation("Message received from Elastic");
 }

        public Task DeleteAsync(List<BsonValue> ids)
        {

            throw new NotImplementedException();
        }


    }
}