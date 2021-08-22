using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents a opening block instruction.
    /// </summary>
    public class BlockInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Block;

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.Open();
        }

        protected override void Serialize(BinaryWriter writer)
        {
        }
    }
}