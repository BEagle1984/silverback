// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util;

internal static class EnumerableSortExtensions
{
    public static IEnumerable<T> SortBySortIndex<T>(this IEnumerable<T> items) =>
        items.OrderBy(item => item, new SortedComparer<T>());

    private sealed class SortedComparer<T> : IComparer<T>
    {
        public int Compare(T? x, T? y) => (x, y) switch
        {
            (ISorted xSorted, ISorted ySorted) => xSorted.SortIndex.CompareTo(ySorted.SortIndex),
            (ISorted xSorted, _) => xSorted.SortIndex > 0 ? 1 : -1,
            (_, ISorted ySorted) => ySorted.SortIndex > 0 ? -1 : 1,
            _ => 0
        };
    }
}
