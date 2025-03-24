// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class SchemaRegistryProxyClassGenerator : ProxyClassGenerator
{
    public SchemaRegistryProxyClassGenerator(Type proxiedType)
        : base(proxiedType, "KafkaSchemaRegistryConfiguration")
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Makes no sense")]
    protected override void MapProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        StringBuilder propertiesStringBuilder = new();
        StringBuilder mapMethodStringBuilder = new();

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType);

            propertiesStringBuilder.AppendSummary(property);

            if (property.Name.EndsWith("Url") && property.PropertyType == typeof(string))
                propertiesStringBuilder.AppendLine("    [SuppressMessage(\"Design\", \"CA1056:URI-like properties should not be strings\", Justification = \"Generated according to wrapped class.\")]");

            propertiesStringBuilder.AppendLine($"    public {propertyType} {property.Name} {{ get; init; }}");
            propertiesStringBuilder.AppendLine();

            mapMethodStringBuilder.AppendLine($"            {property.Name} = {property.Name},");
        }

        StringBuilder.Append(propertiesStringBuilder);
        StringBuilder.AppendLine("    /// <summary>");
        StringBuilder.AppendLine("    ///     Maps to the Confluent client configuration.");
        StringBuilder.AppendLine("    /// </summary>");
        StringBuilder.AppendLine("    /// <returns>");
        StringBuilder.AppendLine("    ///     The Confluent client configuration.");
        StringBuilder.AppendLine("    /// </returns>");
        StringBuilder.AppendLine($"    private {ProxiedType.Name} MapCore() =>");
        StringBuilder.AppendLine("        new()");
        StringBuilder.AppendLine("        {");
        StringBuilder.Append(mapMethodStringBuilder);
        StringBuilder.AppendLine("        };");
    }
}
