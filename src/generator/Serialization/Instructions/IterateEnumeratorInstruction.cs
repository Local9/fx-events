using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an iteration over an enumerator instruction.
    /// </summary>
    public class IterateEnumeratorInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.IterateEnumerator;

        public Identifier Identifier { get; set; }
        public Identifier Enumerator { get; set; }

        public IterateEnumeratorInstruction(Identifier identifier, Identifier enumerator)
        {
            Identifier = identifier;
            Enumerator = enumerator;
        }

        public IterateEnumeratorInstruction(out Identifier identifier, Identifier enumerator)
        {
            identifier = Identifier.CreateIdentifier();

            Identifier = identifier;
            Enumerator = enumerator;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.AppendLine($"foreach (var {Identifier} in {Enumerator})");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Enumerator);
        }
    }
}