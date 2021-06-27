using System;
using CitizenFX.Core.Native;
using JetBrains.Annotations;

namespace Moonlight.Events.Diagnostics
{
    [PublicAPI]
    public abstract class StopwatchUtil
    {
        public abstract TimeSpan Elapsed { get; }
        public abstract void Stop();
        public abstract void Start();

        public static StopwatchUtil StartNew()
        {
            var duplicity = API.IsDuplicityVersion();

            if (!duplicity)
            {
                return new ClientStopwatch();
            }

            return new ServerStopwatch();
        }
    }
}