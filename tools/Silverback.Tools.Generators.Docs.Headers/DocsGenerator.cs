// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Tools.Generators.Docs.Headers;

internal static class DocsGenerator
{
    public static void GenerateDocsTable(Type headersType, string outputFilePath)
    {
        List<string> rows = [];

        foreach (FieldInfo fieldInfo in headersType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (fieldInfo.GetValue(null) is not string key || key.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix))
                continue;

            string apiReferenceLink =
                $"[{fieldInfo.Name}]" +
                $"(xref:{headersType.FullName}" +
                $"#{headersType.FullName!.Replace(".", "_", StringComparison.Ordinal)}" +
                $"_{fieldInfo.Name})";

            rows.Add($"{apiReferenceLink} | `{key}`");
        }

        StringBuilder builder = new();

        if (rows.Count == 0)
        {
            builder.AppendLine("No additional headers are added to the produced messages.");
        }
        else
        {
            builder.AppendLine("Header | Name");
            builder.AppendLine(":-- | :--");

            foreach (string row in rows)
            {
                builder.AppendLine(row);
            }
        }

        File.WriteAllText(outputFilePath, builder.ToString());
    }
}
