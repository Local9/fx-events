using System;
using System.Linq;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Converter
{
    public class EnumerableSerialization : BaseConverter
    {
        private const string EnumerableType = "System.Collections.Generic.IEnumerable`1";
        private const string CollectionType = "System.Collections.Generic.ICollection`1";

        public override bool Criteria(DataType dataType, ITypeSymbol type, string qualified) =>
            HasInterface(type, EnumerableType);

        public override BaseInstruction[] Write(Language language, GenerationHost host,
            TypeSerializationContext context)
        {
            var lengthMember = context.ReferenceMember("Count()");
            var named = (INamedTypeSymbol) context.Type;
            var collectionInterface = GetInterface(context.Type, CollectionType);
            var isCollection = collectionInterface != null;
            var elementType = isCollection
                ? collectionInterface.TypeArguments.Single()
                : named.TypeArguments[0];

            foreach (var member in TypeHelper.GetAllMembers(context.Type))
            {
                if (member.Name is not ("Length" or "Count")) continue;

                lengthMember = context.ReferenceMember(member.Name);

                break;
            }

            return new[]
            {
                new DeclareInstruction(DataType.Int32, out var length, lengthMember),
                new WriteInstruction(length, DataType.Int32),
                new IterateEnumeratorInstruction(out var entry, context.ValueIdentifier).WithChildren(
                    host.GenerateInstructions(language, SerializationFlow.Write, context.Create(elementType, entry)))
            };
        }

        public override BaseInstruction[] Read(Language language, GenerationHost host, TypeSerializationContext context)
        {
            var named = (INamedTypeSymbol) context.Type;
            var collectionInterface = GetInterface(context.Type, CollectionType);
            var isCollection = collectionInterface != null;
            var elementType = isCollection
                ? collectionInterface.TypeArguments.Single()
                : named.TypeArguments[0];

            var collectionType = new TypeIdentifier(DataType.Object,
                isCollection
                    ? TypeHelper.GetFullNameWithArguments(named)
                    : $"{TypeHelper.GetFullNameWithArguments(elementType)}[]")
            {
                IsArray = !isCollection
            };

            var enumerable = Identifier.CreateIdentifier();
            var index = Identifier.CreateIdentifier();
            var indexer = isCollection ? Identifier.CreateIdentifier() : enumerable.ReferenceIndexer(index);

            return new[]
            {
                new ReadInstruction(DataType.Int32, out var length),
                new InstantiateInstruction(enumerable, collectionType, new IValueExpression[] { length }, true),
                new IterateIndexInstruction(index, null, length).WithChildren(
                    Array.Empty<BaseInstruction>()
                        .ConcatIf(isCollection, new DeclareInstruction(new TypeIdentifier(elementType), indexer))
                        .Concat(host.GenerateInstructions(language, SerializationFlow.Read,
                            context.Create(elementType, indexer)))
                        .ConcatIf(isCollection,
                            () => new BaseInstruction[]
                            {
                                new InvokeInstruction(
                                    context.RefIdentifier
                                        .WithTypeCast(new TypeIdentifier(collectionInterface!))
                                        .ReferenceMember("Add"),
                                    new IValueExpression[] { indexer })
                            })
                )
            }.ConcatIf(!isCollection, new AssignInstruction(context.RefIdentifier, enumerable));
        }

        private static bool HasInterface(ITypeSymbol type, string @interface) => GetInterface(type, @interface) != null;

        private static INamedTypeSymbol? GetInterface(ITypeSymbol type, string @interface)
        {
            if (TypeHelper.GetQualifiedName(type) == @interface)
            {
                return (INamedTypeSymbol) type;
            }

            return Enumerable.FirstOrDefault(type.AllInterfaces,
                entry => TypeHelper.GetQualifiedName(entry) == @interface);
        }
    }
}