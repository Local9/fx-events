using JetBrains.Annotations;
using Lusive.Events.Attributes;
using Lusive.Snowflakes;

namespace Lusive.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventResponseMessage : IMessage
    {
        public SnowflakeId Id { get; set; }
        public string Endpoint { get; set; }
        public string Signature { get; set; }
        public byte[] Data { get; set; }

        public EventResponseMessage(SnowflakeId id, string endpoint, string signature, byte[] data)
        {
            Id = id;
            Endpoint = endpoint;
            Signature = signature;
            Data = data;
        }

        public override string ToString() => Id.ToString();
    }
}