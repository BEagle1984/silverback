// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class SchemaRegistryProxyClassGenerator : ProxyClassGenerator
{
    public SchemaRegistryProxyClassGenerator(Type proxiedType)
        : base(proxiedType, "KafkaSchemaRegistryConfiguration")
    {
    }

    protected override void MapProperties()
    {
        IEnumerable<PropertyInfo> properties =
            ReflectionHelper.GetProperties(ProxiedType, true)
                .Where(property => !IgnoredProperties.Contains(property));

        foreach (PropertyInfo property in properties)
        {
            string propertyType = ReflectionHelper.GetTypeString(property.PropertyType, true);

            StringBuilder.AppendSummary(property);

            if (property.Name.EndsWith("Url") && property.PropertyType == typeof(string))
                StringBuilder.AppendLine("    [SuppressMessage(\"Design\", \"CA1056:URI-like properties should not be strings\", Justification = \"Generated according to wrapped class.\")]");

            StringBuilder.AppendLine($"    public {propertyType} {property.Name}");
            StringBuilder.AppendLine("    {");

            if (property.GetGetMethod() != null)
                StringBuilder.AppendLine($"        get => SchemaRegistryConfig.{property.Name};");

            if (property.GetSetMethod() != null)
                StringBuilder.AppendLine($"        init => SchemaRegistryConfig.{property.Name} = value;");

            StringBuilder.AppendLine("    }");
            StringBuilder.AppendLine();
        }
    }
}
