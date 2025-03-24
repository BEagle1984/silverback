// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Silverback.Collections;
using Xunit;

namespace Silverback.Tests.Core.Collections;

public class ValueReadOnlyCollectionFixture
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    public static TheoryData<string[], string[], bool> Equals_ShouldCompareStringsCollection_TestData =>
        new()
        {
            {
                ["a", "b", "c"],
                ["a", "b", "c"],
                true
            },
            {
                ["a", "b", "c"],
                ["a", "b"],
                false
            },
            {
                ["a", "c"],
                ["a", "b", "c"],
                false
            },
            {
                ["a", "b", "c"],
                ["d", "e", "f"],
                false
            }
        };

    [Theory]
    [MemberData(nameof(Equals_ShouldCompareStringsCollection_TestData))]
    public void Equals_ShouldCompareStringsCollection(string[] values1, string[] values2, bool expected)
    {
        ValueReadOnlyCollection<string> collection1 = new(values1);
        ValueReadOnlyCollection<string> collection2 = new(values2);

        collection1.Equals(collection2).ShouldBe(expected);
        collection1.GetHashCode().Equals(collection2.GetHashCode()).ShouldBe(expected);
    }

    [Fact]
    public void Count_ShouldReturnCount()
    {
        ValueReadOnlyCollection<int> collection = new([1, 2, 3]);
        collection.Count.ShouldBe(3);
    }

    [Fact]
    public void Empty_ShouldReturnStaticEmptyCollection()
    {
        ValueReadOnlyCollection<string> empty1 = ValueReadOnlyCollection.Empty<string>();
        ValueReadOnlyCollection<string> empty2 = ValueReadOnlyCollection.Empty<string>();

        empty1.ShouldBeEmpty();
        empty1.ShouldBeSameAs(empty2);
    }
}
