//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using MongoDB.AutoSync.Core.Services;

//namespace MongoDB.AutoSync.Manager.Elastic.Extensions
//{
//    public static class ApplicationBuilderExtensions
//    {
//        public static IApplicationBuilder UseElasticSyncManager(this IApplicationBuilder builder)
//        {
//            var manager = ActivatorUtilities.CreateInstance<ElasticSyncManager>(builder.ApplicationServices);
//            AutoSyncManager.Add(manager);
//            return builder;
//        }
//    }
//}