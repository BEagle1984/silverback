// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableSortExtensionsFixture
{
    [Fact]
    public void SortBySortIndex_ShouldSortItems()
    {
        Item[] items =
        [
            new SortedItem(100),
            new SortedItem(-50),
            new SortedItem(50),
            new SortedItem(-100),
            new("unsorted3"),
            new("unsorted2"),
            new SortedItem(0)
        ];

        IEnumerable<Item> sorted = items.SortBySortIndex();

        sorted.ShouldBe(
        [
            new SortedItem(-100),
            new SortedItem(-50),
            new SortedItem(0),
            new Item("unsorted3"),
            new Item("unsorted2"),
            new SortedItem(50),
            new SortedItem(100)
        ]);
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Needed for testing")]
    private record Item(string Id);

    private sealed record SortedItem(int SortIndex) : Item(SortIndex.ToString(CultureInfo.InvariantCulture)), ISorted;
}
