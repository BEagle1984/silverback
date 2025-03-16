// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using Silverback.Messaging.Messages;

namespace Silverback.Tools.Generators.Docs.Headers;

internal static class DocsGenerator
{
    public static void GenerateDocsTable(Type headersType)
    {
        Console.WriteLine("Header | Name");
        Console.WriteLine(":-- | :--");

        foreach (FieldInfo fieldInfo in headersType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (fieldInfo.GetValue(null) is not string key || key.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix))
                continue;

            string apiReferenceLink =
                $"[{fieldInfo.Name}]" +
                $"(xref:{headersType.FullName}" +
                $"#{headersType.FullName!.Replace(".", "_", StringComparison.Ordinal)}" +
                $"_{fieldInfo.Name})";

            Console.WriteLine($"{apiReferenceLink} | `{key}`");
        }
    }
}
