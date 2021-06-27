using JetBrains.Annotations;

namespace Moonlight.Events.Message
{
    [PublicAPI]
    public class EventParameter
    {
        public byte[] Data { get; set; }

        public EventParameter(byte[] data)
        {
            Data = data;
        }
    }
}