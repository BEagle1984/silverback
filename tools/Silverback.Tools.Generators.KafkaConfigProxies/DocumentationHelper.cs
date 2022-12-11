// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Confluent.Kafka;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

internal static class DocumentationHelper
{
    private static readonly XmlDocument XmlDocumentation = LoadXmlDocumentation();

    public static SummaryText GetSummary(PropertyInfo propertyInfo)
    {
        string path = "P:" + propertyInfo.DeclaringType?.FullName + "." + propertyInfo.Name;
        XmlNode? node = XmlDocumentation.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");

        if (node == null)
            throw new InvalidOperationException($"Unable to find XML documentation for property {path}.");

        StringBuilder stringBuilder = new();
        string? defaultInfo = null;
        string? importance = null;

        foreach (string line in node.InnerXml.Split("\r\n", StringSplitOptions.TrimEntries).Skip(1).SkipLast(1))
        {
            if (line.StartsWith("default: ", StringComparison.Ordinal))
                defaultInfo = $"    /// <br/><br/>{line}";
            else if (line.StartsWith("importance: ", StringComparison.Ordinal))
                importance = $"    /// <br/>{line}";
            else if (!string.IsNullOrEmpty(line))
                stringBuilder.AppendLine($"    /// {line}");
        }

        string? remarks = GetCustomRemarks(propertyInfo.Name);

        return new SummaryText(stringBuilder.ToString(), defaultInfo, importance, remarks);
    }

    private static string? GetCustomRemarks(string propertyName)
    {
        if (propertyName == "DeliveryReportFields")
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("    ///     Silverback overrides this value by default setting it to &quot;key,status&quot; as an optimization,");
            stringBuilder.AppendLine("    ///     since the other fields aren't used.");
            return stringBuilder.ToString();
        }

        return null;
    }

    private static XmlDocument LoadXmlDocumentation()
    {
        Assembly? assembly = Assembly.GetAssembly(typeof(ClientConfig));

        if (assembly == null)
            throw new InvalidOperationException("Couldn't load ClientConfig assembly.");

        string xmlDocumentationPath = Path.Combine(Path.GetDirectoryName(assembly.Location)!, "Confluent.Kafka.xml");

        if (!File.Exists(xmlDocumentationPath))
            throw new InvalidOperationException("Confluent.Kafka.xml file not found.");

        XmlDocument xmlDocumentation = new();
        xmlDocumentation.Load(xmlDocumentationPath);
        return xmlDocumentation;
    }
}
