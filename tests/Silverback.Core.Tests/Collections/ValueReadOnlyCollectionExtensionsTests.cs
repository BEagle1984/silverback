// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Silverback.Collections;
using Xunit;

namespace Silverback.Tests.Core.Collections;

public class ValueReadOnlyCollectionExtensionsTests
{
    [Fact]
    public void AsValueReadOnlyCollection_Enumerable_NewInstanceReturned()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);

        IValueReadOnlyCollection<int> valueCollection = enumerable.AsValueReadOnlyCollection();

        valueCollection.Should().BeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToList().Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void AsValueReadOnlyCollection_List_NewInstanceReturned()
    {
        List<int> list = new() { 1, 2, 3 };

        IValueReadOnlyCollection<int> valueCollection = list.AsValueReadOnlyCollection();

        valueCollection.Should().BeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToList().Should().BeEquivalentTo(list);
    }

    [Fact]
    public void AsValueReadOnlyCollection_Array_NewInstanceReturned()
    {
        int[] array = { 1, 2, 3 };

        IValueReadOnlyCollection<int> valueCollection = array.AsValueReadOnlyCollection();

        valueCollection.Should().BeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToArray().Should().BeEquivalentTo(array);
    }

    [Fact]
    public void AsValueReadOnlyCollection_ValueCollection_SameInstanceReturned()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3, 4 }.AsValueReadOnlyCollection();

        IValueReadOnlyCollection<int> valueCollection = collection.AsValueReadOnlyCollection();

        valueCollection.Should().BeSameAs(collection);
    }
}
