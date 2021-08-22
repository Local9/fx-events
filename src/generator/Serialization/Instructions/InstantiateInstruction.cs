using System.IO;
using System.Linq;
using System.Text;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Expressions;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an object instantiation instruction.
    /// </summary>
    public class InstantiateInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Instantiate;

        public Identifier Identifier { get; set; }
        public TypeIdentifier Type { get; set; }
        public IValueExpression[] Parameters { get; set; }
        public bool Declare { get; set; }

        public InstantiateInstruction(Identifier identifier, TypeIdentifier type, IValueExpression[] parameters,
            bool declare)
        {
            Identifier = identifier;
            Type = type;
            Parameters = parameters;
            Declare = declare;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            var prefix = Declare ? "var " : string.Empty;
            var instantiation = new StringBuilder(Type.ToString());

            if (Type.IsArray)
            {
                instantiation.Remove(instantiation.Length - 1, 1);
                instantiation.Append(Parameters.Single());
                instantiation.Append("]");
            }
            else
            {
                instantiation.Append(string.Join(",", Parameters.Select(self => self.ToString()))
                    .Surround("(", ")"));
            }

            code.AppendLine($"{prefix}{Identifier} = new {instantiation};");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            
            Type.Serialize(writer);
            
            writer.Write(Parameters.Length);

            foreach (var parameter in Parameters)
            {
                writer.Write(parameter.ToString());
            }
            
            writer.Write(Declare);
        }
    }
}