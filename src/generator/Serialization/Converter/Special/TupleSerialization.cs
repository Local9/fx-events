using System.Collections.Generic;
using System.Linq;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter.Special
{
    public class TupleSerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) => qualified.StartsWith("System.Tuple`");

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var items = TypeHelper.GetTypeArgumentCount(context.Type);
            var arguments = ((INamedTypeSymbol) context.Type).TypeArguments;
            var instructions = new List<BaseInstruction[]>();

            for (var index = 0; index < items; index++)
            {
                instructions.Add(host.GenerateInstructions(language, SerializationFlow.Write,
                    context.Create(arguments[index], context.ReferenceMember($"Item{index + 1}"))));
            }

            return Combine(instructions.ToArray());
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host, TypeSerializationContext context)
        {
            var items = TypeHelper.GetTypeArgumentCount(context.Type);
            var arguments = ((INamedTypeSymbol) context.Type).TypeArguments;
            var instructions = new List<BaseInstruction[]>();
            var identifiers = new IValueExpression[items];

            for (var index = 0; index < items; index++)
            {
                instructions.Add(new BaseInstruction[]
                    {
                        new DeclareInstruction(new TypeIdentifier(arguments[index]), out var identifier)
                    }.Concat(host.GenerateInstructions(language, SerializationFlow.Read,
                        context.Create(arguments[index], identifier)))
                    .ToArray());

                identifiers[index] = identifier;
            }

            return Combine(instructions.ToArray(), new BaseInstruction[]
            {
                new InstantiateInstruction(context.RefIdentifier, context.TypeIdentifier, identifiers, false)
            });
        }
    }
}