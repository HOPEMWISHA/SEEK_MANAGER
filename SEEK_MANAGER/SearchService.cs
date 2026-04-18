using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace SEEK_MANAGER
{
    // Simple singleton to synchronize search across forms
    public class SearchService
    {
        private static SearchService _instance;
        public static SearchService Instance => _instance ??= new SearchService();

        private readonly List<Action<string>> subscribers = new List<Action<string>>();

        public void Subscribe(Action<string> handler)
        {
            if (handler == null) return;
            subscribers.Add(handler);
        }

        public void Unsubscribe(Action<string> handler)
        {
            if (handler == null) return;
            subscribers.Remove(handler);
        }

        public void Publish(string query)
        {
            foreach (var s in subscribers)
            {
                try { s(query); } catch { }
            }
        }
    }
}
