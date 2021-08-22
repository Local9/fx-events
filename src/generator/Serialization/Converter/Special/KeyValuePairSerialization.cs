using System.Linq;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter.Special
{
    public class KeyValuePairSerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) =>
            qualified is "System.Collections.Generic.KeyValuePair`2";

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var arguments = ((INamedTypeSymbol) context.Type).TypeArguments;
            var key = host.GenerateInstructions(language, SerializationFlow.Write,
                context.Create(arguments[0], context.ReferenceMember("Key")));

            var value = host.GenerateInstructions(language, SerializationFlow.Write,
                context.Create(arguments[1], context.ReferenceMember("Value")));

            return key.Concat(value).ToArray();
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host, TypeSerializationContext context)
        {
            var arguments = ((INamedTypeSymbol) context.Type).TypeArguments;
            var declarations = new BaseInstruction[]
            {
                new DeclareInstruction(new TypeIdentifier(arguments[0]), out var keyIdentifier),
                new DeclareInstruction(new TypeIdentifier(arguments[1]), out var valueIdentifier),
            };

            var key = host.GenerateInstructions(language, SerializationFlow.Read,
                context.Create(arguments[0], keyIdentifier));
            var value = host.GenerateInstructions(language, SerializationFlow.Read,
                context.Create(arguments[1], valueIdentifier));

            return Combine(declarations, key, value, new BaseInstruction[]
            {
                new InstantiateInstruction(context.RefIdentifier, context.TypeIdentifier,
                    new IValueExpression[] { keyIdentifier, valueIdentifier }, false)
            });
        }
    }
}