// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Silverback.Util;

namespace Silverback.Tests.Performance;

// | Method                | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
// |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
// | LegacySortBySortIndex | 1,313.3 ns | 17.36 ns | 16.24 ns |  1.00 |    0.02 | 0.2575 |    2696 B |        1.00 |
// | SortBySortIndex       |   469.8 ns |  4.97 ns |  4.65 ns |  0.36 |    0.01 | 0.0648 |     680 B |        0.25 |
[SimpleJob]
[MemoryDiagnoser]
public class EnumerableSortExtensionsBenchmark
{
    private readonly IEnumerable<Item> _items = new Item[]
    {
        new SortableItem(1),
        new SortableItem(2),
        new NotSortableItem(),
        new SortableItem(3),
        new NotSortableItem(),
        new SortableItem(4),
        new SortableItem(0),
        new SortableItem(-2),
        new NotSortableItem()
    }.ToPureEnumerable();

    [Benchmark(Baseline = true)]
    public void LegacySortBySortIndex() => _items.LegacySortBySortIndex();

    [Benchmark]
    public void SortBySortIndex() => _items.SortBySortIndex();

    private abstract class Item;

    private class SortableItem : Item, ISorted
    {
        public SortableItem(int sortIndex)
        {
            SortIndex = sortIndex;
        }

        public int SortIndex { get; }
    }

    private class NotSortableItem : Item;
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Test code")]
internal static class EnumerableSortExtensionsCopy
{
    public static IReadOnlyList<T> LegacySortBySortIndex<T>(this IEnumerable<T> items)
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
