using System;

namespace Moonlight.Events.Diagnostics
{
    internal class ClientStopwatch : StopwatchUtil
    {
        private static long Timestamp => (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

        private readonly long _timestamp;
        private long _reduction;
        private long _haltedAt;

        public override TimeSpan Elapsed => new TimeSpan((Timestamp - _timestamp - _reduction) * 10000);

        public ClientStopwatch()
        {
            _timestamp = Timestamp;
        }

        public override void Stop()
        {
            _haltedAt = Timestamp;
        }

        public override void Start()
        {
            if (_haltedAt != 0)
            {
                _reduction = Timestamp - _haltedAt;
            }

            _haltedAt = 0;
        }
    }
}