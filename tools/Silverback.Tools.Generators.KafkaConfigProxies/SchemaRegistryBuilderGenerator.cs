// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class SchemaRegistryBuilderGenerator : BuilderGenerator
{
    public SchemaRegistryBuilderGenerator(Type proxiedType)
        : base(proxiedType, "KafkaSchemaRegistryConfigurationBuilder")
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    protected override void MapClassProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType, true);
            string valueVariableName = property.Name.ToCamelCase();
            string visibility = MustBeInternal(property.Name) ? "internal" : "public partial";

            StringBuilder.AppendLine($"    {visibility} {GeneratedClassName} With{property.Name}({propertyType} {valueVariableName})");
            StringBuilder.AppendLine("    {");
            StringBuilder.AppendLine($"        SchemaRegistryConfig.{property.Name} = {valueVariableName};");
            StringBuilder.AppendLine("        return this;");
            StringBuilder.AppendLine("    }");
            StringBuilder.AppendLine();
        }
    }

    private static bool MustBeInternal(string propertyName) => propertyName is "EnableSslCertificateVerification";
}
