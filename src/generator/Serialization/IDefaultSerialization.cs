using Lusive.Events.Generator.Syntax;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Serialization
{
    public interface IDefaultSerialization
    {
        void Serialize(ISymbol member, ITypeSymbol type, CodeWriter code, string name, string typeIdentifier, Location location);
        void Deserialize(ISymbol member, ITypeSymbol type, CodeWriter code, string name, string typeIdentifier, Location
             location);
    }
}