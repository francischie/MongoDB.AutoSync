using System;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.AutoSync.Core.Extensions;
using MongoDB.AutoSync.Core.Services;
using MongoDB.AutoSync.Manager.Elastic;
using MongoDB.AutoSync.Manager.Elastic.Extensions;

namespace MongoDB.AutoSync.TestApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(o => o.UseMemoryStorage(new MemoryStorageOptions
                {
                    FetchNextJobTimeout = TimeSpan.FromDays(365*100)
                }))
                .AddMvc();

            services.AddMongoAutoSync()
                .AddElasticSyncManager();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer()
                .UseHangfireDashboard()
                .UseMvcWithDefaultRoute()
                .UseElasticSyncManager()
                .UseAutoMongoSync();

        }
    }
}
