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

            AutoSyncManager.Add(new ElasticSyncManager
            {
                CollectionsToSync = new List<string> { "ugc.review" }
            });

            var syncService = ActivatorUtilities.CreateInstance<OplogService>(app.ApplicationServices);
            syncService.StartAsync(new CancellationToken());
        }
    }
}
