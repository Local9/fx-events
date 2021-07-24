using System;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using Lusive.Events.Diagnostics.Impl;

namespace Lusive.Events.Diagnostics
{
    [PublicAPI]
    public abstract class StopwatchUtil
    {
        public static bool IsServer = API.IsDuplicityVersion();
        
        public abstract TimeSpan Elapsed { get; }
        public abstract void Stop();
        public abstract void Start();

        public static long Timestamp => IsServer ? ServerStopwatch.GetTimestamp() : ClientStopwatch.GetTimestamp();

        public static StopwatchUtil StartNew()
        {
            if (IsServer)
            {
                return new ServerStopwatch();
            }

            return new ClientStopwatch();
        }
    }
}