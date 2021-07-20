using JetBrains.Annotations;

namespace Lusive.Events
{
    [PublicAPI]
    public static class EventConstant
    {
        public static readonly string InboundPipeline = "moonlight_event_in";
        public static readonly string OutboundPipeline = "moonlight_event_out";
    }
}