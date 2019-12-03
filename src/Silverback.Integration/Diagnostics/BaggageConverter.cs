// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Diagnostics
{
    internal class BaggageConverter
    {
        private const char _baggageItemSeparator = ',';
        private const char _itemKeyValueSeparator = '=';

        internal static string Serialize(IEnumerable<KeyValuePair<string, string>> baggage)
        {
            return string.Join(_baggageItemSeparator.ToString(), baggage.Select(b => b.Key + _itemKeyValueSeparator + b.Value));
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
                var baggageItemsAsStrings = baggage.Split(_baggageItemSeparator);
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
                var parts = baggageItem.Split(_itemKeyValueSeparator);
                if (parts.Length != 2)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }
        }
    }
}
