using JetBrains.Annotations;

namespace Lusive.Events
{
    [PublicAPI]
    public static class EventConstant
    {
        public static readonly string InvokePipeline = "moonlight_event_a";
        public static readonly string ReplyPipeline = "moonlight_event_b";
        public static readonly string LocalSender = "You";
        public static readonly string ServerSender = "Server";
    }
}