// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Silverback.Diagnostics;

namespace Silverback.Tools.Generators.Docs.LogEvents;

internal static class DocsGenerator
{
    private static readonly HashSet<int> EventIdSet = [];

    public static void GenerateDocsTable(Type logEventsType, string outputFilePath)
    {
        StringBuilder builder = new();

        builder.AppendLine("Id | Level | Message | Reference");
        builder.AppendLine(":-- | :-- | :-- | :--");

        foreach (PropertyInfo property in logEventsType.GetProperties().OrderBy(property => ((LogEvent)property.GetValue(null)!).EventId.Id))
        {
            LogEvent logEvent = (LogEvent)property.GetValue(null)!;

            if (!EventIdSet.Add(logEvent.EventId.Id))
                throw new InvalidOperationException($"Duplicate event id {logEvent.EventId.Id} ({logEventsType.Name}.{property.Name}).");

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
