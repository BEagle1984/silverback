// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util
{
    internal static class EnumerableSortExtensions
    {
        public static IEnumerable<T> SortBySortIndex<T>(this IEnumerable<T> items)
        {
            var list = items.ToList();

            var sortables = list.OfType<ISorted>().OrderBy(sorted => sorted.SortIndex).ToList();
            var notSortables = list.Where(item => !(item is ISorted)).ToList();

            return sortables.Where(sorted => sorted.SortIndex <= 0).Cast<T>()
                .Union(notSortables)
                .Union(sortables.Where(b => b.SortIndex > 0).Cast<T>())
                .ToList();
        }
    }
}
