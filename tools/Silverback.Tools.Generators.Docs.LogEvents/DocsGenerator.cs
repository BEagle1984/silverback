// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using Silverback.Diagnostics;

namespace Silverback.Tools.Generators.Docs.LogEvents;

internal static class DocsGenerator
{
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "Used to ensure id uniqueness")]
    private static readonly HashSet<int> EventIdSet = [];

    public static void GenerateDocsTable(Type logEventsType, string outputFilePath)
    {
        StringBuilder builder = new();

        builder.AppendLine("Id | Level | Message | Reference");
        builder.AppendLine(":-- | :-- | :-- | :--");

        foreach (PropertyInfo property in logEventsType.GetProperties())
        {
            LogEvent logEvent = (LogEvent)property.GetValue(null)!;

            EventIdSet.Add(logEvent.EventId.Id);

            string apiReferenceLink =
                $"[{property.Name}]" +
                $"(xref:{logEventsType.FullName}" +
                $"#{logEventsType.FullName!.Replace(".", "_", StringComparison.Ordinal)}" +
                $"_{property.Name})";

            string message = logEvent.Message.Replace("|", "&#124;", StringComparison.Ordinal);

            builder.AppendLine($"{logEvent.EventId.Id} | {logEvent.Level} | {message} | {apiReferenceLink}");
        }

        File.WriteAllText(outputFilePath, builder.ToString());
    }
}
