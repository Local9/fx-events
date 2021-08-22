using System;
using System.Linq;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter
{
    public class ObjectSerialization : BaseConverter
    {
        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) => true;

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            return GetInstructions(SerializationFlow.Write, language, host, context);
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host, TypeSerializationContext context)
        {
            return GetInstructions(SerializationFlow.Read, language, host, context);
        }

        private static BaseInstruction[] GetInstructions(SerializationFlow flow, Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var identifier = context.GetIdentifier(flow);
            var foreign = language != Language.CSharp || !context.TypeIdentifier.IsSerializable;

            if (!foreign)
            {
                return new BaseInstruction[]
                {
                    new ExtendInstruction(flow, identifier, context.TypeIdentifier)
                };
            }

            var schema = SchemaManager.BuildSchema(context.Type);
            var children = schema.Members.Where(self => flow == SerializationFlow.Write ? self.Write : self.Read)
                .ToArray();
            var useInstantiation =
                TypeHelper.HasConstructor(context.Type, children.Select(self => self.TypeSymbol).ToArray());
            var references = new Identifier[children.Length];

            if (useInstantiation)
            {
                for (var index = 0; index < children.Length; index++)
                {
                    references[index] = Identifier.CreateIdentifier(context.Member.Name.ToCamelCase());
                }
            }

            var identifierCreator = new Func<SchemaMember, int, Identifier>((member, index) =>
                useInstantiation
                    ? references[index]
                    : Identifier.CreateDerivedIdentifier(identifier, member.Name));
            var members = host.Generate(schema, context.Context, identifierCreator);
            var begin = new BeginInstruction(members);

            if (useInstantiation)
            {
                begin = (BeginInstruction) begin.WithChildren(references.Select((reference, index) =>
                    (BaseInstruction) new DeclareInstruction(new TypeIdentifier(children[index].TypeSymbol),
                        reference)).ToArray());
            }
            else if (flow == SerializationFlow.Read)
            {
                begin = (BeginInstruction) begin.WithChildren(new BaseInstruction[]
                {
                    new InstantiateInstruction(identifier, context.TypeIdentifier,
                        Array.Empty<IValueExpression>(), false)
                });
            }

            return new BaseInstruction[] { begin }.ConcatIf(useInstantiation,
                new InstantiateInstruction(identifier, context.TypeIdentifier,
                    references.Cast<IValueExpression>().ToArray(), false));
        }
    }
}