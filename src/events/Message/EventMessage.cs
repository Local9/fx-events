using System.Collections.Generic;
using JetBrains.Annotations;
using Moonlight.Events.Attributes;
using Moonlight.Snowflakes;

namespace Moonlight.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventMessage : IMessage
    {
        public Snowflake Id { get; set; }
        public string Signature { get; set; }
        public string Endpoint { get; set; }
        public EventFlowType Flow { get; set; }
        public IEnumerable<EventParameter> Parameters { get; set; }

        public EventMessage(string endpoint, EventFlowType flow, IEnumerable<EventParameter> parameters)
        {
            Id = Snowflake.Next();
            Endpoint = endpoint;
            Flow = flow;
            Parameters = parameters;
        }

        public override string ToString() => Endpoint;
    }
}