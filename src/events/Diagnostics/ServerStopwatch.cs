using System;
using System.Diagnostics;

namespace Moonlight.Events.Diagnostics
{
    internal class ServerStopwatch : StopwatchUtil
    {
        private readonly Stopwatch _stopwatch;
        public override TimeSpan Elapsed => _stopwatch.Elapsed;

        public ServerStopwatch()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public override void Stop()
        {
            _stopwatch.Stop();
        }

        public override void Start()
        {
            _stopwatch.Start();
        }
    }
}