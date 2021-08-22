using System.IO;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents a declaration of an inline variable.
    /// </summary>
    public class DeclareInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Declare;

        public Identifier Identifier { get; }
        public TypeIdentifier Type { get; }
        public IValueExpression Value { get; }

        public DeclareInstruction(TypeIdentifier type, Identifier identifier, IValueExpression? value = null)
        {
            Type = type;
            Identifier = identifier;
            Value = value ?? ConstantExpression.DefaultKeyword;
        }

        public DeclareInstruction(TypeIdentifier type, out Identifier identifier, IValueExpression? value = null) : this(type, Identifier.CreateIdentifier(), value)
        {
            identifier = Identifier;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            code.AppendLine($"{Type} {Identifier} = {Value};");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            
            Type.Serialize(writer);
            
            writer.Write(Value.ToString());
        }
    }
}