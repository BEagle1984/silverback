// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util;

internal static class EnumerableSortExtensions
{
    public static IReadOnlyList<T> SortBySortIndex<T>(this IEnumerable<T> items)
    {
        List<T> notSortables = [];
        List<ISorted> sortables = [];

        foreach (T item in items)
        {
            if (item is ISorted sortedItem)
                sortables.Add(sortedItem);
            else
                notSortables.Add(item);
        }

        sortables.Sort((x, y) => x.SortIndex.CompareTo(y.SortIndex));

        List<T> result = new(sortables.Count + notSortables.Count);
        result.AddRange(sortables.Where(sorted => sorted.SortIndex <= 0).Cast<T>());
        result.AddRange(notSortables);
        result.AddRange(sortables.Where(sorted => sorted.SortIndex > 0).Cast<T>());

        return result;
    }
}
