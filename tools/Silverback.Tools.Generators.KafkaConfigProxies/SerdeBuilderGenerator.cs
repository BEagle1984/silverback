// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public class SerdeBuilderGenerator : BuilderGenerator
{
    public SerdeBuilderGenerator(Type proxiedType)
        : base(proxiedType)
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    protected override void MapClassProperties()
    {
        IEnumerable<PropertyInfo> properties = SerdeProperties.Get(ProxiedType);

        StringBuilder fieldsStringBuilder = new();
        StringBuilder propertiesStringBuilder = new();
        StringBuilder buildMethodStringBuilder = new();

        foreach (PropertyInfo property in properties)
        {
            string propertyType = SerdeProperties.GetTypeString(property);
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

            // The dictionaries are copied to prevent the built configuration from changing when the original dictionary is modified
            if (SerdeProperties.IsDictionary(property))
                buildMethodStringBuilder.AppendLine($"            {property.Name} = {field}?.ToDictionary(pair => pair.Key, pair => pair.Value),");
            else
                buildMethodStringBuilder.AppendLine($"            {property.Name} = {field},");
        }

        StringBuilder.Append(fieldsStringBuilder);
        StringBuilder.Append(propertiesStringBuilder);
        StringBuilder.AppendLine($"    private {BuiltTypeName} BuildCore() =>");
        StringBuilder.AppendLine("        new()");
        StringBuilder.AppendLine("        {");
        StringBuilder.Append(buildMethodStringBuilder);
        StringBuilder.AppendLine("        };");
    }

    // The boolean settings are exposed through the Enable/Disable methods declared in the hand-written part
    private static bool MustBeInternal(string propertyName) =>
        propertyName
            is "AutoRegisterSchemas"
            or "NormalizeSchemas"
            or "UseLatestVersion"
            or "LatestCompatibilityStrict"
            or "Validate"
            or "ValidateBeforeDomainRules"
            or "SkipKnownTypes"
            or "UseDeprecatedFormat"
            or "ValidationRulesFailFast";
}
