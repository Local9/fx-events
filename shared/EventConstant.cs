using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Events
{
    [PublicAPI]
    public static class EventConstant
    {
        public static readonly string InboundPipeline = "moonlight_rpc_in";
        public static readonly string OutboundPipeline = "moonlight_rpc_out";
        public static readonly string SignaturePipeline = "moonlight_sig";
    }
}