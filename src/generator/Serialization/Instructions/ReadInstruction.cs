using System;
using System.Collections.Generic;
using System.IO;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization.Instructions
{
    /// <summary>
    /// Represents a single primitive type read instruction.
    /// </summary>
    public class ReadInstruction : BaseInstruction
    {
        public override InstructionType InstructionType => InstructionType.Read;

        public Identifier Identifier { get; }
        public TypeIdentifier Type { get; }
        public bool Instantiate { get; }

        public ReadInstruction(Identifier identifier, TypeIdentifier type)
        {
            Identifier = identifier;
            Type = type;
        }

        public ReadInstruction(TypeIdentifier type, out Identifier identifier)
        {
            identifier = Identifier.CreateIdentifier();

            Type = type;
            Identifier = identifier;
            Instantiate = true;
        }

        public override void Build(GenerationContext context, InstructionHost host, SchemaMember member,
            CodeWriter code)
        {
            var primitives = GetPrimitiveOverload(context);
            var overload = primitives.TryGetValue(Type.DataType, out var found) ? found : "Unk";
            var cast = Type.DataType == DataType.Enum ? Type.ToString().Surround("(", ")") : string.Empty;
            var prefix = Instantiate ? "var " : string.Empty;

            code.AppendLine($"{prefix}{Identifier} = {cast}{context.Reader}.Read{overload}();");
        }

        protected override void Serialize(BinaryWriter writer)
        {
            writer.Write(Identifier);

            Type.Serialize(writer);

            writer.Write(Instantiate);
        }

        private static Dictionary<DataType, string> GetPrimitiveOverload(GenerationContext context)
        {
            return context.Language switch
            {
                Language.CSharp => new Dictionary<DataType, string>
                {
                    { DataType.Boolean, "Bool" },
                    { DataType.Char, "Char" },
                    { DataType.SByte, "SByte" },
                    { DataType.Byte, "Byte" },
                    { DataType.Int16, "Int16" },
                    { DataType.UInt16, "UInt16" },
                    { DataType.Int32, "Int32" },
                    { DataType.UInt32, "UInt32" },
                    { DataType.Int64, "Int64" },
                    { DataType.UInt64, "UInt64" },
                    { DataType.Decimal, "Decimal" },
                    { DataType.Single, "Single" },
                    { DataType.Double, "Double" },
                    { DataType.String, "String" },
                    { DataType.Enum, "Int32" }
                },
                _ => throw GeneratorConstant.UnsupportedLanguage(context.Language, nameof(GetPrimitiveOverload))
            };
        }
    }
}