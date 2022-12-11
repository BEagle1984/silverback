// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Test methods")]
public class EnumerableAsCollectionExtensionsFixture
{
    [Fact]
    public void AsReadOnlyCollection_ShouldReturnNewEquivalentCollection_WhenEnumerableIsPassed()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 10);

        IReadOnlyCollection<int> collection = enumerable.AsReadOnlyCollection();

        collection.Should().NotBeSameAs(enumerable);
        collection.Should().BeEquivalentTo(enumerable);
    }

    [Fact]
    public void AsReadOnlyCollection_ShouldReturnSameObject_WhenListIsPassed()
    {
        List<int> enumerable = new() { 1, 2, 3, 4 };

        IReadOnlyCollection<int> collection = enumerable.AsReadOnlyCollection();

        collection.Should().BeSameAs(enumerable);
    }

    [Fact]
    public void AsReadOnlyList_ShouldReturnNewEquivalentList_WhenEnumerableIsPassed()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 10);

        IReadOnlyList<int> list = enumerable.AsReadOnlyList();

        list.Should().NotBeSameAs(enumerable);
        list.Should().BeEquivalentTo(enumerable);
    }

    [Fact]
    public void AsReadOnlyList_ShouldReturnSameObject_WhenListIsPassed()
    {
        int[] enumerable = { 1, 2, 3, 4 };

        IReadOnlyList<int> list = enumerable.AsReadOnlyList();

        list.Should().BeSameAs(enumerable);
    }

    [Fact]
    public void AsList_ShouldReturnNewEquivalentList_WhenEnumerableIsPassed()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 10);

        List<int> list = enumerable.AsList();

        list.Should().NotBeSameAs(enumerable);
        list.Should().BeEquivalentTo(enumerable);
    }

    [Fact]
    public void AsList_ShouldReturnSameObject_WhenListIsPassed()
    {
        List<int> enumerable = new() { 1, 2, 3, 4 };

        List<int> list = enumerable.AsList();

        list.Should().BeSameAs(enumerable);
    }

    [Fact]
    public void AsArray_ShouldReturnNewEquivalentArray_WhenEnumerableIsPassed()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 10);

        int[] array = enumerable.AsArray();

        array.Should().NotBeSameAs(enumerable);
        array.Should().BeEquivalentTo(enumerable);
    }

    [Fact]
    public void AsArray_ShouldReturnSameObject_WhenArrayIsPassed()
    {
        int[] enumerable = { 1, 2, 3, 4 };

        int[] array = enumerable.AsArray();

        array.Should().BeSameAs(enumerable);
    }
}
