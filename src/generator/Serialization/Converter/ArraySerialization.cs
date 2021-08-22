using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter
{
    public class ArraySerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) => type.TypeKind is TypeKind.Array;

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var type = (IArrayTypeSymbol) context.Type;
            var length = context.ReferenceArrayLength();
            var index = Identifier.CreateIdentifier();

            return new[]
            {
                new WriteInstruction(length, DataType.Int32),
                new IterateIndexInstruction(index, null, length).WithChildren(host.GenerateInstructions(
                    language, SerializationFlow.Write,
                    context.Create(type.ElementType, context.ReferenceIndexer(index))))
            };
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var type = (IArrayTypeSymbol) context.Type;
            var index = Identifier.CreateIdentifier();

            return new[]
            {
                new ReadInstruction(DataType.Int32, out var length),
                new InstantiateInstruction(context.RefIdentifier, context.TypeIdentifier,
                    new IValueExpression[] { length }, false),
                new IterateIndexInstruction(index, null, length).WithChildren(host.GenerateInstructions(
                    language, SerializationFlow.Read,
                    context.Create(type.ElementType, context.ReferenceIndexer(index))))
            };
        }
    }
}