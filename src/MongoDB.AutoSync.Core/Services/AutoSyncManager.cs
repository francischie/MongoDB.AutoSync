using System;
using System.Collections.Generic;

namespace MongoDB.AutoSync.Core.Services
{
    public class AutoSyncManager
    {
        private static readonly  Lazy<AutoSyncManager> Instance = new Lazy<AutoSyncManager>();

        private readonly List<IDocManager> _managers = new List<IDocManager>();

        public static void Add(IDocManager manager)
        {
            Instance.Value._managers.Add(manager);
        }

        public static List<IDocManager> Managers => Instance.Value._managers;


    }
}