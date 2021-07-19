using Microsoft.CodeAnalysis;
using Moonlight.Generators.Syntax;

namespace Moonlight.Generators.Serialization
{
    public interface IDefaultSerialization
    {
        void Serialize(ISymbol member, ITypeSymbol type, CodeWriter code, string name, string typeIdentifier, Location location);
        void Deserialize(ISymbol member, ITypeSymbol type, CodeWriter code, string name, string typeIdentifier, Location
             location);
    }
}