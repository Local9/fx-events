using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization
{
    public abstract class BaseInstruction
    {
        public abstract InstructionType InstructionType { get; }
        public BaseInstruction[]? Children { get; private set; }

        public BaseInstruction WithChildren(BaseInstruction[] instructions)
        {
            Children = instructions;

            return this;
        }

        public void WriteBuffer(BinaryWriter writer)
        {
            writer.Write((int) InstructionType);

            Serialize(writer);

            writer.Write(Children != null);

            if (Children == null) return;

            writer.Write(Children.Length);

            foreach (var child in Children)
            {
                child.WriteBuffer(writer);
            }
        }

        public abstract void Build(GenerationContext context, InstructionHost host, SchemaMember member, CodeWriter code);
        protected abstract void Serialize(BinaryWriter writer);
    }
}