// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util
{
    /// <summary>
    ///     <para>
    ///         This comparer is meant to be used for configuration dictionaries only.
    ///     </para>
    ///     <para>
    ///         Nome of the applied rules:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     a null dictionary is equal to an empty dictionary
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     entries with null values (or <c>default(TValue)</c>) and equivalent to completely missing entries
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     the default comparer is used for values
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     multiple entries with the same key are not allowed
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </summary>
    internal class ConfigurationDictionaryComparer<TKey, TValue>
        : IEqualityComparer<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        public bool Equals(
            IEnumerable<KeyValuePair<TKey, TValue>>? dictionaryX,
            IEnumerable<KeyValuePair<TKey, TValue>>? dictionaryY)
        {
            dictionaryX = (dictionaryX ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).ToList();
            dictionaryY = (dictionaryY ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).ToList();

            if (dictionaryX.Count() != dictionaryY.Count())
                return false;

            var allKeys = dictionaryX.Select(pair => pair.Key)
                .Union(
                    dictionaryY.Select(pair => pair.Key))
                .Distinct()
                .ToList();

            if (allKeys.Count != dictionaryX.Count())
                return false;

            foreach (var key in allKeys)
            {
                var valueX = dictionaryX.FirstOrDefault(pair => Equals(pair.Key, key));
                var valueY = dictionaryY.FirstOrDefault(pair => Equals(pair.Key, key));

                if (!Equals(valueX, valueY))
                    return false;
            }

            return true;
        }

        public int GetHashCode(IEnumerable<KeyValuePair<TKey, TValue>> dictionary) =>
            dictionary.Count();
    }
}