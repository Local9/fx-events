using System;
using System.Collections.Generic;
using System.Linq;

namespace Lusive.Events.Generator
{
    public static class Logger
    {
        public static List<string> Buffer { get; } = new();
        
        private static int ScopeLevel { get; set; }
        private static readonly ScopeTracker Tracker = new();

        public static void Info(params string[] messages)
        {
            var message = string.Join(Environment.NewLine, messages.Select(self =>
            {
                var formatted = new string('\t', ScopeLevel) + self;

                return formatted;
            }));
            
            Console.WriteLine(message);
            Buffer.Add(message);
        }

        public static ScopeTracker Scope()
        {
            ScopeLevel++;

            return Tracker;
        }

        public class ScopeTracker : IDisposable
        {
            public void Dispose()
            {
                ScopeLevel--;
            }
        }
    }
}