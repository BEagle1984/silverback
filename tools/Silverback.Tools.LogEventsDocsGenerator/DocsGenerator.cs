// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Diagnostics;

namespace Silverback.Tools.LogEventsDocsGenerator
{
    internal static class DocsGenerator
    {
        private static readonly HashSet<int> EventIdSet = new();

        public static void GenerateDocsTable(Type logEventsType)
        {
            Console.WriteLine("Id | Level | Message | Reference");
            Console.WriteLine(":-- | :-- | :-- | :--");

            foreach (var property in logEventsType.GetProperties())
            {
                var logEvent = (LogEvent)property.GetValue(null)!;

                EventIdSet.Add(logEvent.EventId.Id);

                var apiReferenceLink =
                    $"[{property.Name}]" +
                    $"(xref:{logEventsType.FullName}" +
                    $"#{logEventsType.FullName!.Replace(".", "_", StringComparison.Ordinal)}" +
                    $"_{property.Name})";

                var message = logEvent.Message.Replace("|", "&#124;", StringComparison.Ordinal);

                Console.WriteLine(
                    $"{logEvent.EventId.Id} | {logEvent.Level} | {message} | {apiReferenceLink}");
            }
        }
    }
}
