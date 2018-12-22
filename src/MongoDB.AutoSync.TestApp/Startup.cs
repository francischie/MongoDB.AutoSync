using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Extensions;
using MongoDB.AutoSync.Core.Services;
using MongoDB.AutoSync.Manager.Elastic;

namespace MongoDB.AutoSync.TestApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMongoAutoSync(o => { })
                .AddHangfire(o => o.UseMemoryStorage(new MemoryStorageOptions
                {
                    FetchNextJobTimeout = TimeSpan.FromDays(365*100)
                }))
                .AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer()
                .UseHangfireDashboard()
                .UseMvcWithDefaultRoute();


            var manager = ActivatorUtilities.CreateInstance<ElasticSyncManager>(app.ApplicationServices);
            manager.CollectionsToSync = new List<string> {"ugc.review"};
            AutoSyncManager.Add(manager);

            var manager2 = ActivatorUtilities.CreateInstance<MsSqlSyncManager>(app.ApplicationServices);
            manager2.CollectionsToSync = new List<string> { "ugc.review" };
            AutoSyncManager.Add(manager2);


            var syncService = ActivatorUtilities.CreateInstance<MongoReplicationService>(app.ApplicationServices);
            syncService.StartAsync(new CancellationToken());
        }
    }
}
