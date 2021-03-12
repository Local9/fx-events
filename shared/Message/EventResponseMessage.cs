using JetBrains.Annotations;
using Moonlight.Shared.Internal.Json;

namespace Moonlight.Shared.Internal.Events.Message
{
    [PublicAPI]
    public class EventResponseMessage : ISerializable
    {
        public Snowflake Id { get; set; }
        public string Signature { get; set; }
        public string Serialized { get; set; }

        public EventResponseMessage(Snowflake id, string signature, string serialized)
        {
            Id = id;
            Signature = signature;
            Serialized = serialized;
        }

        public string Serialize()
        {
            return this.ToJson();
        }

        public static EventResponseMessage Deserialize(string serialized)
        {
            return serialized.FromJson<EventResponseMessage>();
        }
    }
}