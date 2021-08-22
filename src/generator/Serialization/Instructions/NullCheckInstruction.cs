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
    /// Represents a null check instruction.
    /// </summary>
    public class NullCheckInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.NullCheck;

        public Identifier Identifier { get; }
        public bool NullableType { get; }

        public NullCheckInstruction(Identifier identifier, bool nullableType)
        {
            Identifier = identifier;
            NullableType = nullableType;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            var technique = GetNullCheckTechnique(Identifier, NullableType, context);

            if (context.Flow == SerializationFlow.Write)
            {
                code.AppendLine($"{context.Writer}.Write({technique});");
                code.AppendLine($"if ({technique})");
                code.Open();
            }
            else
            {
                code.AppendLine($"if ({context.Reader}.ReadBoolean())");
                code.Open();
            }
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);
            writer.Write(NullableType);
        }

        public string GetNullCheckTechnique(Identifier identifier, bool nullableValue, GenerationContext context)
        {
            return context.Language switch
            {
                Language.CSharp => nullableValue
                    ? identifier.WithoutReference().ReferenceMember("HasValue")
                    : $"{identifier} is not null",
                _ => throw GeneratorConstant.UnsupportedLanguage(context.Language, nameof(GetNullCheckTechnique))
            };
        }
    }
}