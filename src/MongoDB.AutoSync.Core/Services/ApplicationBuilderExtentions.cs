using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MongoDB.AutoSync.Core.Services
{
    public static class ApplicationBuilderExtentions
    {
        public static IApplicationBuilder UseAutoMongoSync(this IApplicationBuilder builder)
        {
            var syncService = ActivatorUtilities.CreateInstance<AutoMongoSyncService>(builder.ApplicationServices);
            syncService.StartAsync();
            return builder;
        }
    }
}