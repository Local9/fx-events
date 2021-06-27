using JetBrains.Annotations;
using Moonlight.Events.Attributes;

namespace Moonlight.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventParameter
    {
        public byte[] Data { get; set; }

        public EventParameter(byte[] data)
        {
            Data = data;
        }
    }
}