using System;
using System.IO;
using Lusive.Events.Generator.Generation;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization.Identifiers
{
    public struct TypeIdentifier
    {
        public DataType DataType { get; }
        private string? FullName { get; }
        public bool IsArray { get; set; }
        public bool IsSerializable { get; set; }

        public TypeIdentifier(ITypeSymbol type)
        {
            DataType = TypeHelper.GetDataType(type);
            FullName = DataType is DataType.Object or DataType.Enum
                ? TypeHelper.GetFullNameWithArguments(type)
                : null;
            IsArray = type.TypeKind == TypeKind.Array;
            IsSerializable = TypeHelper.IsSerializable(type);
        }

        public TypeIdentifier(DataType dataType, string? fullName = null, bool serializable = false)
        {
            DataType = dataType;
            FullName = fullName;
            IsArray = false;
            IsSerializable = serializable;
        }

        public override string ToString()
        {
            if (DataType is DataType.None or DataType.Object or DataType.Enum)
            {
                return FullName!;
            }

            return DataType switch
            {
                DataType.Boolean => "bool",
                DataType.Char => "char",
                DataType.SByte => "sbyte",
                DataType.Byte => "byte",
                DataType.Int16 => "short",
                DataType.UInt16 => "ushort",
                DataType.Int32 => "int",
                DataType.UInt32 => "uint",
                DataType.Int64 => "long",
                DataType.UInt64 => "ulong",
                DataType.Decimal => "decimal",
                DataType.Single => "float",
                DataType.Double => "double",
                DataType.String => "string",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int) DataType);
            writer.Write(FullName != null);

            if (FullName != null)
            {
                writer.Write(FullName);
            }

            writer.Write(IsArray);
        }

        public static implicit operator TypeIdentifier(DataType dataType) => new(dataType);
    }
}