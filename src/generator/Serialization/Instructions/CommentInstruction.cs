using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    public class CommentInstruction : BaseInstruction
    {
        public CommentInstruction(string comment)
        {
            Comment = comment;
        }

        public override InstructionType InstructionType => InstructionType.Comment;
        public string Comment { get; set; }
        
        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member, CodeWriter code)
        {
            code.AppendLine($"// {Comment}");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Comment);
        }
    }
}