// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class SerdeProxyClassGenerator : ProxyClassGenerator
{
    public SerdeProxyClassGenerator(Type proxiedType)
        : base(proxiedType)
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Makes no sense")]
    protected override void MapProperties()
    {
        IEnumerable<PropertyInfo> properties = SerdeProperties.Get(ProxiedType);

        StringBuilder propertiesStringBuilder = new();
        StringBuilder initializerStringBuilder = new();
        StringBuilder conditionalMappingStringBuilder = new();

        foreach (PropertyInfo property in properties)
        {
            propertiesStringBuilder.AppendSummary(property);
            propertiesStringBuilder.AppendLine($"    public {SerdeProperties.GetTypeString(property)} {property.Name} {{ get; init; }}");
            propertiesStringBuilder.AppendLine();

            if (SerdeProperties.IsDictionary(property))
            {
                conditionalMappingStringBuilder.AppendLine();
                conditionalMappingStringBuilder.AppendLine($"        if ({property.Name} != null)");
                conditionalMappingStringBuilder.AppendLine($"            confluentConfig.{property.Name} = {property.Name}.ToDictionary(pair => pair.Key, pair => pair.Value);");
            }
            else if (SerdeProperties.IsNonNullableValueType(property))
            {
                conditionalMappingStringBuilder.AppendLine();
                conditionalMappingStringBuilder.AppendLine($"        if ({property.Name} != null)");
                conditionalMappingStringBuilder.AppendLine($"            confluentConfig.{property.Name} = {property.Name}.Value;");
            }
            else
            {
                initializerStringBuilder.AppendLine($"            {property.Name} = {property.Name},");
            }
        }

        StringBuilder.Append(propertiesStringBuilder);
        StringBuilder.AppendLine("    /// <summary>");
        StringBuilder.AppendLine("    ///     Maps to the Confluent serializer or deserializer configuration.");
        StringBuilder.AppendLine("    /// </summary>");
        StringBuilder.AppendLine("    /// <returns>");
        StringBuilder.AppendLine("    ///     The Confluent serializer or deserializer configuration.");
        StringBuilder.AppendLine("    /// </returns>");
        StringBuilder.AppendLine($"    private {ProxiedType.FullName} MapCore()");
        StringBuilder.AppendLine("    {");
        StringBuilder.AppendLine($"        {ProxiedType.FullName} confluentConfig = new()");
        StringBuilder.AppendLine("        {");
        StringBuilder.Append(initializerStringBuilder);
        StringBuilder.AppendLine("        };");
        StringBuilder.Append(conditionalMappingStringBuilder);
        StringBuilder.AppendLine();
        StringBuilder.AppendLine("        return confluentConfig;");
        StringBuilder.AppendLine("    }");
    }
}
