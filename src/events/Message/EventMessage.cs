using System.Collections.Generic;
using JetBrains.Annotations;
using Lusive.Events.Attributes;
using Lusive.Snowflake;

namespace Lusive.Events.Message
{
    [PublicAPI]
    [Serialization]
    public partial class EventMessage : IMessage
    {
        public SnowflakeId Id { get; set; }
        public string? Signature { get; set; }
        public string Endpoint { get; set; }
        public EventFlowType FlowType { get; set; }
        public IEnumerable<EventParameter> Parameters { get; set; }
        // TODO: Debug stuff
        // public string ReadonlyName { get; }
        // public List<Encapsulated> NullableParameters { get; set; }
        // public Encapsulated DeepList { get; set; }
        // public Vector3 Vector { get; set; }
        // public string String { get; set; }
        // public string? NullableString { get; set; }
        // public Nullable<int> NullableInt { get; set; }
        // public string[] StringArray { get; set; }
        // public string[]? NullableStringArray { get; set; }
        // public EventParameter[] DeepArray { get; set; }
        // public IEnumerable<EventParameter> DeepEnumerable { get; set; }
        // public Dictionary<int, EventParameter> DeepDictionary { get; set; }

        public EventMessage(string endpoint, EventFlowType flowType, IEnumerable<EventParameter> parameters)
        {
            Id = SnowflakeId.Next();
            Endpoint = endpoint;
            FlowType = flowType;
            Parameters = parameters;
        }

        public override string ToString() => Endpoint;
    }
}