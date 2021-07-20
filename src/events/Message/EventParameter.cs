using JetBrains.Annotations;
using Lusive.Events.Attributes;

namespace Lusive.Events.Message
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