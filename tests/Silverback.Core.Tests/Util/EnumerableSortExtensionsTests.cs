// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class EnumerableSortExtensionsTests
    {
        [Fact]
        public void SortBySortIndex_SomeItems_SortedAsExpected()
        {
            var items = new[]
            {
                new SortedItem(100),
                new SortedItem(-50),
                new SortedItem(50),
                new SortedItem(-100),
                new Item("unsorted3"),
                new Item("unsorted2")
            };

            var sorted = items.SortBySortIndex();

            sorted.Should().BeEquivalentTo(
                new[]
                {
                    new SortedItem(-100),
                    new SortedItem(-50),
                    new Item("unsorted3"),
                    new Item("unsorted2"),
                    new SortedItem(50),
                    new SortedItem(100)
                });
        }

        private class Item
        {
            public Item(string id) => Id = id;

            public string Id { get; }
        }

        private sealed class SortedItem : Item, ISorted
        {
            public SortedItem(int sortIndex)
                : base(sortIndex.ToString(CultureInfo.InvariantCulture)) => SortIndex = sortIndex;

            public int SortIndex { get; }
        }
    }
}
