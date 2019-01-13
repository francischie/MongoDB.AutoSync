using System;
using System.Collections.Generic;

namespace MongoDB.AutoSync.Core.Services
{
    public class AutoSyncManagers
    {
        private static readonly  Lazy<AutoSyncManagers> Instance = new Lazy<AutoSyncManagers>();

        private readonly List<IDocManager> _managers = new List<IDocManager>();

        public static void Add(IDocManager manager)
        {
            Instance.Value._managers.Add(manager);
        }

        public static List<IDocManager> Managers => Instance.Value._managers;


    }
}