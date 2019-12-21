// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util
{
    internal static class EnumerableSortExtensions
    {
        public static IEnumerable<T> SortBySortIndex<T>(this IEnumerable<T> items)
        {
            var sorted = items.OfType<ISorted>().OrderBy(b => b.SortIndex).ToList();
            var unsortable = items.Where(b => !(b is ISorted)).ToList();

            return sorted
                .Where(b => b.SortIndex <= 0).Cast<T>()
                .Union(unsortable)
                .Union(sorted.Where(b => b.SortIndex > 0).Cast<T>())
                .ToList();
        }
    }
}