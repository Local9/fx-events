using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter.Special
{
    public class TimeSerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) =>
            qualified is "System.DateTime" or "System.TimeSpan";

        public override BaseInstruction[] Write(Language language,GenerationHost host, TypeSerializationContext context)
        {
            return new BaseInstruction[]
            {
                new WriteInstruction(context.ReferenceMember("Ticks"), DataType.Int64)
            };
        }

        public override BaseInstruction[] Read(Language language,GenerationHost host, TypeSerializationContext context)
        {
            return new BaseInstruction[]
            {
                new ReadInstruction(DataType.Int64, out var identifier),
                new InstantiateInstruction(context.RefIdentifier, context.TypeIdentifier,
                    new IValueExpression[] { identifier }, false)
            };
        }
    }
}