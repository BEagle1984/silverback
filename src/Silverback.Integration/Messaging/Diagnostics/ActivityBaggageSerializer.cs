// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Silverback.Messaging.Diagnostics;

internal static class ActivityBaggageSerializer
{
    private const char BaggageItemSeparator = ',';

    private const char ItemKeyValueSeparator = '=';

    public static string Serialize(IEnumerable<KeyValuePair<string, string?>> baggage)
    {
        // Rents a pooled buffer only when the stack-allocated one is too small
        DefaultInterpolatedStringHandler handler = new(0, 0, CultureInfo.InvariantCulture, stackalloc char[256]);
        bool isFirstItem = true;

        foreach ((string key, string? value) in baggage)
        {
            if (!isFirstItem)
                handler.AppendFormatted(BaggageItemSeparator);

            handler.AppendFormatted(key);
            handler.AppendFormatted(ItemKeyValueSeparator);
            handler.AppendFormatted(value);
            isFirstItem = false;
        }

        return handler.ToStringAndClear();
    }

    public static IReadOnlyCollection<KeyValuePair<string, string>> Deserialize(string baggage)
    {
        if (string.IsNullOrEmpty(baggage))
            return [];

        List<KeyValuePair<string, string>> items = [];

        foreach (Range itemRange in baggage.AsSpan().Split(BaggageItemSeparator))
        {
            ReadOnlySpan<char> item = baggage.AsSpan(itemRange);
            int separatorIndex = item.IndexOf(ItemKeyValueSeparator);

            // Skip the items without exactly one key/value separator
            if (separatorIndex < 0 || item[(separatorIndex + 1)..].Contains(ItemKeyValueSeparator))
                continue;

            items.Add(
                new KeyValuePair<string, string>(
                    item[..separatorIndex].Trim().ToString(),
                    item[(separatorIndex + 1)..].Trim().ToString()));
        }

        return items;
    }
}
