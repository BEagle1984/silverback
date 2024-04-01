// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Collections;
using Xunit;

namespace Silverback.Tests.Core.Collections;

public class ValueReadOnlyCollectionTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    [SuppressMessage("ReSharper", "CA2208", Justification = "Test")]
    public static IEnumerable<object[]> Equals_StringCollection_CorrectResultReturned_TestData =>
    [
        [new[] { "a", "b", "c" }, new[] { "a", "b", "c" }, true],
        [new[] { "a", "b", "c" }, new[] { "a", "b" }, false],
        [new[] { "a", "c" }, new[] { "a", "b", "c" }, false],
        [new[] { "a", "b", "c" }, new[] { "d", "e", "f" }, false]
    ];

    [Theory]
    [MemberData(nameof(Equals_StringCollection_CorrectResultReturned_TestData))]
    public void Equals_StringCollection_CorrectResultReturned(string[] values1, string[] values2, bool expected)
    {
        ValueReadOnlyCollection<string> collection1 = new(values1);
        ValueReadOnlyCollection<string> collection2 = new(values2);

        collection1.Equals(collection2).Should().Be(expected);
        collection1.GetHashCode().Equals(collection2.GetHashCode()).Should().Be(expected);
    }

    [Fact]
    public void Count_CorrectValueReturned()
    {
        ValueReadOnlyCollection<int> collection = new([1, 2, 3]);
        collection.Count.Should().Be(3);
    }

    [Fact]
    public void Empty_StaticEmptyCollectionReturned()
    {
        ValueReadOnlyCollection<string> empty1 = ValueReadOnlyCollection.Empty<string>();
        ValueReadOnlyCollection<string> empty2 = ValueReadOnlyCollection.Empty<string>();

        empty1.Should().BeEmpty();
        empty1.Should().BeSameAs(empty2);
    }
}
