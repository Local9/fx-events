using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lusive.Events.Generator.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lusive.Events.Generator.Generation
{
    public static class TypeHelper
    {
        public static string GetFullNameWithArguments(ITypeSymbol type)
        {
            var builder = new StringBuilder();
            var named = GetNamedTypeSymbol(type);

            builder.Append(GetFullName(named));

            if (named.TypeArguments != null && !named.TypeArguments.IsDefaultOrEmpty)
            {
                builder.Append("<");
                builder.Append(string.Join(",",
                    named.TypeArguments
                        .Select(GetFullNameWithArguments)));
                builder.Append(">");
            }

            if (type.TypeKind == TypeKind.Array)
            {
                builder.Append("[]");
            }

            return builder.ToString();
        }

        public static INamedTypeSymbol GetNamedTypeSymbol(ITypeSymbol type)
        {
            return type switch
            {
                INamedTypeSymbol named => named,
                IArrayTypeSymbol array => GetNamedTypeSymbol(array.ElementType),
                _ => throw new ArgumentOutOfRangeException($"Could not get INamedTypeSymbol: {type.Name}")
            };
        }

        public static ITypeSymbol GetNonNullableType(ITypeSymbol type)
        {
            var nullable = IsTypeNullable(type);

            if (!nullable) return type;
            if (GetQualifiedName(type) == GeneratorConstant.NullableType)
            {
                return ((INamedTypeSymbol) type).TypeArguments.Single();
            }

            return type.WithNullableAnnotation(NullableAnnotation.None);
        }

        public static bool IsTypeNullable(ITypeSymbol type)
        {
            return IsTypeNullableType(type) ||
                   type.NullableAnnotation == NullableAnnotation.Annotated;
        }

        public static bool IsTypeNullableType(ITypeSymbol type)
        {
            return GetQualifiedName(type) == GeneratorConstant.NullableType;
        }

        public static bool IsPrimitive(ITypeSymbol type)
        {
            var dataType = GetDataType(type);

            return dataType is not DataType.None and not DataType.Enum and not DataType.Object;
        }

        public static DataType GetDataType(ITypeSymbol symbol)
        {
            var type = GetNonNullableType(symbol);

            if (type.TypeKind is TypeKind.Enum) return DataType.Enum;

            return type.SpecialType switch
            {
                SpecialType.System_Boolean => DataType.Boolean,
                SpecialType.System_Char => DataType.Char,
                SpecialType.System_SByte => DataType.SByte,
                SpecialType.System_Byte => DataType.Byte,
                SpecialType.System_Int16 => DataType.Int16,
                SpecialType.System_UInt16 => DataType.UInt16,
                SpecialType.System_Int32 => DataType.Int32,
                SpecialType.System_UInt32 => DataType.UInt32,
                SpecialType.System_Int64 => DataType.Int64,
                SpecialType.System_UInt64 => DataType.UInt64,
                SpecialType.System_Decimal => DataType.Decimal,
                SpecialType.System_Single => DataType.Single,
                SpecialType.System_Double => DataType.Double,
                SpecialType.System_String => DataType.String,
                _ => DataType.Object
            };
        }

        public static bool HasEmptyConstructor(SyntaxNode declaration)
        {
            return declaration.DescendantNodes().Any(self =>
                self is ConstructorDeclarationSyntax constructorDecl &&
                constructorDecl.ParameterList.Parameters.Count == 0);
        }

        public static bool HasConstructor(ITypeSymbol type, ITypeSymbol[] parameters)
        {
            var named = GetNamedTypeSymbol(type);

            foreach (var constructor in named.Constructors)
            {
                if (constructor.Parameters.Length != parameters.Length) continue;

                var match = true;
                
                for (var index = 0; index < constructor.Parameters.Length; index++)
                {
                    var parameter = constructor.Parameters[index];
                    var equivalent = parameters[index];

                    if (parameter.Type.MetadataName == equivalent.MetadataName) continue;
                    
                    match = false;
                    
                    break;
                }

                if (!match) continue;
                
                return true;
            }

            return false;
        }

        public static int GetTypeArgumentCount(ISymbol symbol)
        {
            var qualified = GetQualifiedName(symbol);
            var index = qualified.LastIndexOf("`", StringComparison.Ordinal);

            return index != -1 ? int.Parse(qualified.Substring(index)) : 0;
        }

        /// <summary>
        /// Gets the full name of the supplied <see cref="ISymbol"/>, ex:
        /// <code>
        /// Namespace.Type
        /// </code>
        /// </summary>
        /// <param name="symbol">The symbol to analyze.</param>
        /// <returns>The full name string.</returns>
        public static string GetFullName(ISymbol symbol)
        {
            var builder = new StringBuilder();
            var containing = symbol;

            builder.Append(symbol.ContainingNamespace);
            builder.Append(".");

            var idx = builder.Length;

            while ((containing = containing.ContainingType) != null)
            {
                builder.Insert(idx, containing.Name + ".");
            }

            builder.Append(symbol.Name);

            return builder.ToString();
        }

        /// <summary>
        /// Gets the full name of the supplied <see cref="ISymbol"/> including type arguments, ex:
        /// <code>
        /// Namespace.Type`1
        /// </code>
        /// </summary>
        /// <param name="symbol">The symbol to analyze.</param>
        /// <returns>The qualified name string.</returns>
        public static string GetQualifiedName(ISymbol symbol)
        {
            var name = GetFullName(symbol);

            if (symbol is not INamedTypeSymbol { TypeArguments: { Length: > 0 } } named) return name;

            name += "`";
            name += named.TypeArguments.Length;

            return name;
        }

        /// <summary>
        /// If the supplied <see cref="ISymbol"/> is marked as serializable, and should be processed in the generator.
        /// </summary>
        /// <param name="symbol">The type to check.</param>
        /// <returns>True if marked with the [Serialization] attribute.</returns>
        public static bool IsSerializable(ISymbol symbol)
        {
            var attribute = symbol.GetAttributes()
                .FirstOrDefault(self => self.AttributeClass is { Name: "SerializationAttribute" });

            return attribute != null;
        }

        /// <summary>
        /// If the supplied <see cref="ITypeSymbol"/> has a method with the supplied signature.
        /// </summary>
        /// <param name="symbol">The type to check.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <returns>True if a method exists with the supplied signature.</returns>
        public static bool HasMethod(ITypeSymbol symbol, string methodName,
            params string[]? parameters)
        {
            foreach (var member in GetAllMembers(symbol))
            {
                if (member is not IMethodSymbol methodSymbol || methodSymbol.Name != methodName) continue;
                if (parameters == null || parameters.Length == 0) return true;

                var failed = false;

                for (var index = 0; index < parameters.Length; index++)
                {
                    var parameter = parameters[index];

                    if (methodSymbol.Parameters.Length == index)
                    {
                        failed = true;

                        break;
                    }

                    if (GetQualifiedName(methodSymbol.Parameters[index].Type) == parameter) continue;

                    failed = true;

                    break;
                }

                if (failed) continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all members including members in the base type and interfaces in the supplied <see cref="ITypeSymbol"/>.
        /// </summary>
        /// <param name="symbol">The symbol to analyze.</param>
        /// <returns>An collection of all members visible in the supplied type.</returns>
        public static IEnumerable<ISymbol> GetAllMembers(ITypeSymbol symbol)
        {
            var members = new List<ISymbol>();

            members.AddRange(symbol.GetMembers());

            if (symbol.BaseType != null)
                members.AddRange(
                    symbol.BaseType.GetMembers().Where(self => members.All(deep => self.Name != deep.Name)));

            foreach (var type in symbol.AllInterfaces)
            {
                members.AddRange(type.GetMembers().Where(self => members.All(deep => self.Name != deep.Name)));
            }

            return members.Where(self => !self.IsStatic);
        }
    }
}