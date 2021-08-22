using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents a single primitive type write instruction.
    /// </summary>
    public class WriteInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Write;

        public Identifier Reference { get; set; }
        public TypeIdentifier Type { get; set; }

        public WriteInstruction(Identifier @ref, TypeIdentifier type)
        {
            Reference = @ref;
            Type = type;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.AppendLine($"{context.Writer}.Write({Reference});");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Reference);
            
            Type.Serialize(writer);
        }
    }
}