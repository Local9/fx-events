using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization
{
    public class TypeSerializationContext
    {
        public GenerationContext Context { get; }
        public SchemaMember Member { get; }
        public Identifier ValueIdentifier { get; }
        public Identifier RefIdentifier { get; }
        public ITypeSymbol OriginalType { get; }
        public ITypeSymbol Type => TypeHelper.GetNonNullableType(OriginalType);
        public bool IsTypeNullable => TypeHelper.IsTypeNullable(OriginalType);
        public TypeIdentifier TypeIdentifier { get; }
        public Location? Location { get; }

        public Identifier GetIdentifier(SerializationFlow flow)
        {
            return flow == SerializationFlow.Write ? ValueIdentifier : RefIdentifier;
        }

        public TypeSerializationContext(GenerationContext context, SchemaMember member, ITypeSymbol type,
            Identifier identifier)
        {
            RefIdentifier = identifier;
            ValueIdentifier = TypeHelper.IsTypeNullableType(type) ? identifier.ReferenceMember("Value") : identifier;
            
            Context = context;
            Member = member;
            OriginalType = type;
            TypeIdentifier = new TypeIdentifier(TypeHelper.GetNonNullableType(type));
            Location = member.Location;
        }
        
        public TypeSerializationContext(GenerationContext context, SchemaMember member, Identifier identifier) : this(
            context, member, member.TypeSymbol, identifier)
        {
        }

        public Identifier ReferenceMember(string member) => ValueIdentifier.ReferenceMember(member);
        public Identifier ReferenceIndexer(string indexer) => ValueIdentifier.ReferenceIndexer(indexer);

        public Identifier ReferenceArrayLength()
        {
            var length = Context.Language switch
            {
                Language.CSharp => "Length",
                Language.JavaScript => "length",
                _ => throw GeneratorConstant.UnsupportedLanguage(Context.Language, nameof(ReferenceArrayLength))
            };

            return ReferenceMember(length);
        }

        public TypeSerializationContext Create(ITypeSymbol type, Identifier identifier)
        {
            return new TypeSerializationContext(Context, Member, type, identifier);
        }
    }
}