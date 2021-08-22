using System;
using System.IO;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents an instruction where the serialization extends into the passed object.
    /// </summary>
    public class ExtendInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Extend;

        public SerializationFlow Flow { get; set; }
        public Identifier Identifier { get; set; }
        public TypeIdentifier TypeIdentifier { get; set; }

        public ExtendInstruction(SerializationFlow flow, Identifier identifier, TypeIdentifier typeIdentifier)
        {
            Flow = flow;
            Identifier = identifier;
            TypeIdentifier = typeIdentifier;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            switch (Flow)
            {
                case SerializationFlow.Write:
                    code.AppendLine($"{Identifier}.PackSerializedBytes({context.Writer});");

                    break;
                case SerializationFlow.Read:
                    code.AppendLine($"{Identifier} = new {TypeIdentifier}({context.Reader});");

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write((int) Flow);
            writer.Write(Identifier);

            TypeIdentifier.Serialize(writer);
        }
    }
}