using Lusive.Events.Generator.Serialization;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Schemas
{
    public struct SchemaMember
    {
        public static readonly SchemaMember Empty = new();
        public Schema Parent;
        public string Name;
        public DataType DataType;
        public bool Write;
        public bool Read;
        public bool IsNullable;
        public Schema InnerSchema;
        public ITypeSymbol TypeSymbol;
        public Location? Location;
    }
}