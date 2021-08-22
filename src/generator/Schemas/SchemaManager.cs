using System;
using System.Collections.Generic;
using System.Linq;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Serialization;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Schemas
{
    public static class SchemaManager
    {
        public static Schema BuildSchema(ITypeSymbol symbol)
        {
            try
            {
                var properties = new List<SchemaMember>();
                var overrides = new List<string>();
                var schema = new Schema(TypeHelper.GetQualifiedName(symbol));

                Logger.Info($"Building schema for type: {symbol.Name}");

                using (Logger.Scope())
                {
                    foreach (var location in symbol.Locations)
                    {
                        Logger.Info($"- Location: {location}");
                    }

                    foreach (var member in TypeHelper.GetAllMembers(symbol))
                    {
                        if (member is not (IPropertySymbol or IFieldSymbol)) continue;
                        if (overrides.Contains(member.Name)) continue;
                        if (member.IsOverride)
                        {
                            overrides.Add(member.Name);
                        }

                        var attributes = member.GetAttributes();
                        var ignored = attributes.FirstOrDefault(self => self.AttributeClass is
                            { Name: "IgnoreAttribute" });
                        var isWriteIgnored = ignored != null && ignored.GetAttributeValue("Write", true);
                        var isReadIgnored = ignored != null && ignored.GetAttributeValue("Read", true);

                        if (isReadIgnored && isWriteIgnored) continue;

                        var forced =
                            attributes.FirstOrDefault(self => self.AttributeClass is { Name: "ForceAttribute" });
                        var isWriteForced = forced != null && forced.GetAttributeValue("Write", true);
                        var isReadForced = forced != null && forced.GetAttributeValue("Read", true);

                        if (!isReadForced && !isWriteForced &&
                            member.DeclaredAccessibility != Accessibility.Public) continue;
                        if (member is IPropertySymbol { IsIndexer: true }) continue;

                        var isReadOnly = member switch
                        {
                            IFieldSymbol field => field.IsReadOnly,
                            IPropertySymbol property => property.IsReadOnly,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        var isWriteOnly = member switch
                        {
                            IFieldSymbol => false,
                            IPropertySymbol property => property.IsWriteOnly,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        var type = member switch
                        {
                            IFieldSymbol field => field.Type,
                            IPropertySymbol property => property.Type,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        var dataType = TypeHelper.GetDataType(type);
                        var location = member.Locations.FirstOrDefault();

                        if (location.IsInMetadata)
                        {
                            location = symbol.Locations.FirstOrDefault();
                        }

                        Logger.Info($"Member: {member.Name} ({location})");

                        var entry = new SchemaMember
                        {
                            Parent = schema,
                            Name = member.Name,
                            DataType = dataType,
                            Write = !isWriteIgnored && (!isWriteOnly || isWriteForced),
                            Read = !isReadIgnored && (!isReadOnly || isReadForced),
                            IsNullable = TypeHelper.IsTypeNullable(type),
                            TypeSymbol = type,
                            Location = location
                        };

                        if (dataType == DataType.Object)
                        {
                            // entry.InnerSchema = BuildSchema(type);
                        }

                        properties.Add(entry);
                    }

                    schema.Members = properties.ToArray();

                    return schema;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    $"Unknown exception occured during schema build: {symbol.Name} -> {exception.Message}");
            }
        }
    }
}