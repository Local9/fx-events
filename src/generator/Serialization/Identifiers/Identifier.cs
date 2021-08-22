using System;
using Lusive.Events.Generator.Serialization.Expressions;

namespace Lusive.Events.Generator.Serialization.Identifiers
{
    public class Identifier : IValueExpression
    {
        public string Name { get; private set; }
        public object Value => Name;

        private Identifier()
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public Identifier WithTypeCast(TypeIdentifier typeCast)
        {
            return CreateReferencingIdentifier($"(({typeCast}){Name})");
        }

        public Identifier WithoutReference()
        {
            var dotIndex = Name.LastIndexOf(".", StringComparison.Ordinal);

            return dotIndex == -1 ? this : CreateReferencingIdentifier(Name.Substring(0, dotIndex));
        }

        public Identifier ReferenceMember(string member) =>
            CreateReferencingIdentifier($"{this}.{member}");

        public Identifier ReferenceIndexer(string indexer) =>
            CreateReferencingIdentifier($"{this}[{indexer}]");

        public static Identifier CreateIdentifier(string? name = null)
        {
            return new Identifier
            {
                Name = IdentifierContext.CreateIdentifier(name ?? "var")
            };
        }

        public static Identifier CreateReferencingIdentifier(string name) => new()
        {
            Name = name
        };

        public static Identifier CreateDerivedIdentifier(Identifier baseIdentifier, string name)
        {
            return baseIdentifier.ReferenceMember(name);
        }

        public static implicit operator string(Identifier identifier) => identifier.ToString();
    }
}