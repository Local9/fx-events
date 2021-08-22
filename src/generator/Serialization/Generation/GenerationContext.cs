using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Serialization.Identifiers;

namespace Lusive.Events.Generator.Serialization.Generation
{
    public readonly struct GenerationContext
    {
        public Language Language { get; }
        public SerializationFlow Flow { get; }
        public Identifier Writer { get; }
        public Identifier Reader { get; }

        public GenerationContext(Language language, SerializationFlow flow,  Identifier writer, Identifier reader)
        {
            Language = language;
            Flow = flow;
            Writer = writer;
            Reader = reader;
        }
    }
}