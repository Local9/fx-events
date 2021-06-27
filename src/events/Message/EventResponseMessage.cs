using JetBrains.Annotations;
using Moonlight.Events.Attributes;
using Moonlight.Snowflakes;

namespace Moonlight.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventResponseMessage : IMessage
    {
        public Snowflake Id { get; set; }
        public string Signature { get; set; }
        public byte[] Data { get; set; }

        public EventResponseMessage(Snowflake id, string signature, byte[] data)
        {
            Id = id;
            Signature = signature;
            Data = data;
        }

        public override string ToString() => Id.ToString();
    }
}