// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Shouldly;
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

        valueCollection.ShouldBeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToList().ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void AsValueReadOnlyCollection_List_NewInstanceReturned()
    {
        List<int> list = [1, 2, 3];

        IValueReadOnlyCollection<int> valueCollection = list.AsValueReadOnlyCollection();

        valueCollection.ShouldBeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToList().ShouldBe(list);
    }

    [Fact]
    public void AsValueReadOnlyCollection_Array_NewInstanceReturned()
    {
        int[] array = [1, 2, 3];

        IValueReadOnlyCollection<int> valueCollection = array.AsValueReadOnlyCollection();

        valueCollection.ShouldBeOfType<ValueReadOnlyCollection<int>>();
        valueCollection.ToArray().ShouldBe(array);
    }

    [Fact]
    public void AsValueReadOnlyCollection_ValueCollection_SameInstanceReturned()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3, 4 }.AsValueReadOnlyCollection();

        IValueReadOnlyCollection<int> valueCollection = collection.AsValueReadOnlyCollection();

        valueCollection.ShouldBeSameAs(collection);
    }
}
