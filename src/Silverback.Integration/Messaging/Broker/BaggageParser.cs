// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    // TODO: Test
    internal class BaggageParser
    {
        private const char _itemKeyValueSeparator = '=';
        private const char _baggageItemSeparator = ',';

        internal static bool TryParse(string baggage, out IEnumerable<KeyValuePair<string, string>> baggageItems)
        {
            baggageItems = null;
            IList<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            if (string.IsNullOrEmpty(baggage))
            {
                return false;
            }

            string[] baggageItemsAsStrings = baggage.Split(_baggageItemSeparator);
            foreach (var baggageItem in baggageItemsAsStrings)
            {
                string[] parts = baggageItem.Split(_itemKeyValueSeparator);
                if (parts.Length != 2)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
            }

            if(result.Count < 0)
            {
                return false;
            }

            baggageItems = result;
            return true;
        }
    }
}