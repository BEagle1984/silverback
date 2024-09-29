// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util;

internal static class EnumerableSortExtensions
{
    public static IReadOnlyList<T> SortBySortIndex<T>(this IEnumerable<T> items)
    {
        List<T> list = items.ToList();

        List<ISorted> sortables = [.. list.OfType<ISorted>().OrderBy(sorted => sorted.SortIndex)];
        List<T> notSortables = list.Where(item => item is not ISorted).ToList();

        return sortables.Where(sorted => sorted.SortIndex <= 0).Cast<T>()
            .Union(notSortables)
            .Union(sortables.Where(b => b.SortIndex > 0).Cast<T>())
            .ToList();
    }
}
