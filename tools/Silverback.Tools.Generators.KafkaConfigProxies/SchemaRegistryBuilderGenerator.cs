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

public class SchemaRegistryBuilderGenerator : BuilderGenerator
{
    public SchemaRegistryBuilderGenerator(Type proxiedType)
        : base(proxiedType)
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    protected override void MapClassProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        StringBuilder fieldsStringBuilder = new();
        StringBuilder propertiesStringBuilder = new();
        StringBuilder buildMethodStringBuilder = new();

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType);
            string argument = property.Name.ToCamelCase();
            string field = $"_{argument}";
            string visibility = MustBeInternal(property.Name) ? "internal" : "public partial";

            fieldsStringBuilder.AppendLine($"    private {propertyType} {field};");

            propertiesStringBuilder.AppendLine($"    {visibility} {GeneratedClassName} With{property.Name}({propertyType} {argument})");
            propertiesStringBuilder.AppendLine("    {");
            propertiesStringBuilder.AppendLine($"        {field} = {argument};");
            propertiesStringBuilder.AppendLine("        return this;");
            propertiesStringBuilder.AppendLine("    }");
            propertiesStringBuilder.AppendLine();

            buildMethodStringBuilder.AppendLine($"            {property.Name} = {field},");
        }

        StringBuilder.Append(fieldsStringBuilder);
        StringBuilder.Append(propertiesStringBuilder);
        StringBuilder.AppendLine($"    private {BuiltTypeName} BuildCore() =>");
        StringBuilder.AppendLine($"        new()");
        StringBuilder.AppendLine("        {");
        StringBuilder.Append(buildMethodStringBuilder);
        StringBuilder.AppendLine("        };");
    }

    private static bool MustBeInternal(string propertyName) => propertyName is "EnableSslCertificateVerification";
}
