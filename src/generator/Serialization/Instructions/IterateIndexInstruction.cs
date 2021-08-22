using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an iteration over indices instruction.
    /// </summary>
    public class IterateIndexInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.IterateIndex;

        public Identifier Identifier { get; set; }
        public Identifier? Initial { get; set; }
        public Identifier Count { get; set; }

        public IterateIndexInstruction(Identifier identifier, Identifier? initial, Identifier count)
        {
            Identifier = identifier;
            Initial = initial;
            Count = count;
        }

        public IterateIndexInstruction(out Identifier identifier, Identifier? initial, Identifier count)
        {
            identifier = Identifier.CreateIdentifier();

            Identifier = identifier;
            Initial = initial;
            Count = count;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.AppendLine($"for (int {Identifier} = {Initial ?? "0"}; {Identifier} < {Count}; {Identifier}++)");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Initial != null);
            
            if (Initial != null)
            {
                writer.Write(Initial);
            }
            
            writer.Write(Count);
        }
    }
}