using System;
using Lusive.Events.Generator.Serialization;

namespace Lusive.Events.Generator
{
    public static class GeneratorConstant
    {
        public const string Notice = "// Auto-generated by the Serialization Generator.";
        public const string PackingMethod = "PackSerializedBytes";
        public const string UnpackingMethod = "UnpackSerializedBytes";
        public const string InstructionBufferMethod = "GetInstructionBuffer";
        public const string NullableType = "System.Nullable`1";

        public static NotSupportedException UnsupportedLanguage(Language language, string location)
        {
            return new NotSupportedException($"The language {Enum.GetName(typeof(Language), language)} is not supported in this operation ({location}).");
        }
    }
}