using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter
{
    public class EnumSerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) =>
            dataType == DataType.Enum;

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            return new BaseInstruction[]
            {
                new WriteInstruction(context.ValueIdentifier.WithTypeCast(DataType.Int32), DataType.Int32)
            };
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host, TypeSerializationContext context)
        {
            return new BaseInstruction[]
            {
                new ReadInstruction(context.RefIdentifier, context.TypeIdentifier)
            };
        }
    }
}