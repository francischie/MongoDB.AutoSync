using System;
using System.Threading;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Services;

namespace MongoDB.AutoSync.TestApp
{
    public class HangfireLongProcessJob
    {
        private readonly IServiceProvider _service;

        public HangfireLongProcessJob(IServiceProvider service)
        {
            _service = service;
        }

        public void Start(IJobCancellationToken jobCancellation)
        {
            var syncService = ActivatorUtilities.CreateInstance<AutoMongoSyncService>(_service);
            var token = syncService.StartAsync();
            while (true)
            {
                try
                {
                    jobCancellation.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
                catch (OperationCanceledException)
                {
                    token.Cancel();
                    return;
                }
            }
        }
    }
}