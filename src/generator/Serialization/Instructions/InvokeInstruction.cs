using System.IO;
using System.Linq;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an method invocation instruction.
    /// </summary>
    public class InvokeInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Invoke;

        public Identifier Identifier { get; }
        public IValueExpression[] Parameters { get; }

        public InvokeInstruction(Identifier identifier, IValueExpression[] parameters)
        {
            Identifier = identifier;
            Parameters = parameters;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            var parameters = string.Join(",", Parameters.Select(self => self.ToString()));

            code.AppendLine($"{Identifier}({parameters});");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(Parameters.Length);

            foreach (var parameter in Parameters)
            {
                writer.Write(parameter.ToString());
            }
        }
    }
}