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
    ///                     entries with null values (or <c>default(TValue)</c>) and equivalent to completely
    ///                     missing entries
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
    internal sealed class ConfigurationDictionaryEqualityComparer<TKey, TValue>
        : IEqualityComparer<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        public bool Equals(IEnumerable<KeyValuePair<TKey, TValue>>? x, IEnumerable<KeyValuePair<TKey, TValue>>? y)
        {
            x = (x ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).ToList();
            y = (y ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).ToList();

            if (x.Count() != y.Count())
                return false;

            var hashY = y.ToDictionary(pair => pair.Key, pair => pair.Value);

            return x.All(pairX => hashY.TryGetValue(pairX.Key, out var valueY) && Equals(pairX.Value, valueY));
        }

        public int GetHashCode(IEnumerable<KeyValuePair<TKey, TValue>> obj) => obj.Count();
    }
}
