using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MongoDB.AutoSync.Core.Services
{
    public static class ApplicationBuilderExtensions
    {

        /// <summary>
        /// Start autosync service on non-blocking state
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAutoMongoSync(this IApplicationBuilder builder)
        {
            var syncService = ActivatorUtilities.CreateInstance<AutoMongoSyncService>(builder.ApplicationServices);
            syncService.StartAutoSync();
            return builder;
        }
    }
}