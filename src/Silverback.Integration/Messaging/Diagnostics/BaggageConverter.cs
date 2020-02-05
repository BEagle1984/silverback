// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Diagnostics
{
    internal static class BaggageConverter
    {
        private const char BaggageItemSeparator = ',';
        private const char ItemKeyValueSeparator = '=';

        internal static string Serialize(IEnumerable<KeyValuePair<string, string>> baggage)
        {
            return string.Join(BaggageItemSeparator.ToString(),
                baggage.Select(b => b.Key + ItemKeyValueSeparator + b.Value));
        }

        internal static bool TryDeserialize(string baggage, out IEnumerable<KeyValuePair<string, string>> baggageItems)
        {
            baggageItems = null;
            IList<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            if (string.IsNullOrEmpty(baggage))
            {
                return false;
            }

            try
            {
                var baggageItemsAsStrings = baggage.Split(BaggageItemSeparator);
                Deserialize(result, baggageItemsAsStrings);

                if (result.Count < 0)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // Do nothing.
            }

            baggageItems = result;
            return true;
        }

        private static void Deserialize(IList<KeyValuePair<string, string>> result, string[] baggageItemsAsStrings)
        {
            foreach (var baggageItem in baggageItemsAsStrings)
            {
                var parts = baggageItem.Split(ItemKeyValueSeparator);
                if (parts.Length != 2)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }
        }
    }
}