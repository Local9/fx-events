using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an assignment to a member.
    /// </summary>
    public class AssignInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Assign;

        public Identifier Identifier { get; }
        public IValueExpression Value { get; }

        public AssignInstruction(Identifier identifier, IValueExpression value)
        {
            Identifier = identifier;
            Value = value;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.AppendLine($"{Identifier} = {Value};");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Value.ToString());
        }
    }
}