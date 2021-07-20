using System.Collections.Generic;
using JetBrains.Annotations;
using Lusive.Events.Attributes;
using Lusive.Snowflakes;

namespace Lusive.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventMessage : IMessage
    {
        public SnowflakeId Id { get; set; }
        public string Signature { get; set; }
        public string Endpoint { get; set; }
        public EventFlowType Flow { get; set; }
        public IEnumerable<EventParameter> Parameters { get; set; }

        public EventMessage(string endpoint, EventFlowType flow, IEnumerable<EventParameter> parameters)
        {
            Id = SnowflakeId.Next();
            Endpoint = endpoint;
            Flow = flow;
            Parameters = parameters;
        }

        public override string ToString() => Endpoint;
    }
}