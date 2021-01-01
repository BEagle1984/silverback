// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Silverback.Messaging.Diagnostics
{
    internal static class ActivityBaggageSerializer
    {
        private const char BaggageItemSeparator = ',';

        private const char ItemKeyValueSeparator = '=';

        [SuppressMessage("", "CA1822", Justification = "Uniform to Deserialize")]
        public static string Serialize(IEnumerable<KeyValuePair<string, string?>> baggage) =>
            string.Join(
                BaggageItemSeparator.ToString(CultureInfo.InvariantCulture),
                baggage.Select(b => b.Key + ItemKeyValueSeparator + b.Value));

        public static IReadOnlyCollection<KeyValuePair<string, string>> Deserialize(string baggage)
        {
            if (string.IsNullOrEmpty(baggage))
                return Array.Empty<KeyValuePair<string, string>>();

            var baggageItemsAsStrings = baggage.Split(BaggageItemSeparator);
            return Deserialize(baggageItemsAsStrings).ToList();
        }

        private static IEnumerable<KeyValuePair<string, string>> Deserialize(string[] baggageItemsAsStrings)
        {
            foreach (var baggageItem in baggageItemsAsStrings)
            {
                var parts = baggageItem.Split(ItemKeyValueSeparator);
                if (parts.Length == 2)
                    yield return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}
