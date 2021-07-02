using Microsoft.CodeAnalysis;
using Moonlight.Generators.Syntax;

namespace Moonlight.Generators.Serialization
{
    public class KeyValuePairSerialization : IDefaultSerialization
    {
        public void Serialize(SerializationEngine engine, IPropertySymbol property, ITypeSymbol type, CodeWriter code,
            string name, string typeIdentifier, Location location)
        {
            var named = (INamedTypeSymbol) type;
            var types = named.TypeArguments;

            engine.AppendWriteLogic(property, types[0], code, $"{name}.Key", location);
            engine.AppendWriteLogic(property, types[1], code, $"{name}.Value", location);
        }

        public void Deserialize(SerializationEngine engine, IPropertySymbol property, ITypeSymbol type, CodeWriter code,
            string name, string typeIdentifier, Location location)
        {
            var named = (INamedTypeSymbol) type;
            var types = named.TypeArguments;

            code.AppendLine($"{SerializationEngine.GetQualifiedName(types[0])} key = default;");
            code.AppendLine($"{SerializationEngine.GetQualifiedName(types[1])} value = default;");

            engine.AppendReadLogic(property, types[0], code, "key", location);
            engine.AppendReadLogic(property, types[1], code, "value", location);

            code.AppendLine($"{name} = new {typeIdentifier}(key, value);");
        }
    }
}