// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class CollectionEqualityComparerTests
{
    [Fact]
    public void Equals_SamePrimitivesCollection_TrueReturned()
    {
        int[] collection = { 1, 2, 3, 4, 5 };

        bool result = new CollectionEqualityComparer<int>().Equals(collection, collection);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SamePrimitives_TrueReturned()
    {
        int[] collection1 = { 1, 2, 3, 4, 5 };
        int[] collection2 = { 1, 2, 3, 4, 5 };

        bool result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SamePrimitivesDifferentOrder_TrueReturned()
    {
        int[] collection1 = { 1, 2, 3, 4, 5 };
        int[] collection2 = { 1, 5, 3, 4, 2 };

        bool result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentPrimitives_FalseReturned()
    {
        int[] collection1 = { 1, 2, 3, 4, 5 };
        int[] collection2 = { 1, 2, 4, 5, 6 };

        bool result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameObjectsCollection_TrueReturned()
    {
        IEvent[] collection =
        {
            new TestEventOne { Message = "one" },
            new TestEventOne { Message = "two" },
            new TestEventTwo()
        };

        bool result = new CollectionEqualityComparer<IEvent>().Equals(collection, collection);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameObjects_TrueReturned()
    {
        TestEventOne event1 = new() { Message = "one" };
        TestEventOne event2 = new() { Message = "two" };
        TestEventOne event3 = new() { Message = "three" };

        TestEventOne[] collection1 =
        {
            event1,
            event2,
            event3
        };
        TestEventOne[] collection2 =
        {
            event1,
            event2,
            event3
        };

        bool result =
            new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message).Equals(
                collection1,
                collection2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameItemsDifferentOrder_TrueReturned()
    {
        TestEventOne event1 = new() { Message = "one" };
        TestEventOne event2 = new() { Message = "two" };
        TestEventOne event3 = new() { Message = "three" };

        TestEventOne[] collection1 =
        {
            event1,
            event2,
            event3
        };
        TestEventOne[] collection2 =
        {
            event2,
            event1,
            event3
        };

        bool result = new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message)
            .Equals(collection1, collection2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentObjects_FalseReturned()
    {
        TestEventOne event1 = new() { Message = "one" };
        TestEventOne event2 = new() { Message = "two" };
        TestEventOne event3 = new() { Message = "three" };

        TestEventOne[] collection1 =
        {
            event1,
            event3
        };
        TestEventOne[] collection2 =
        {
            event1,
            event2,
            event3
        };

        bool result = new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message)
            .Equals(collection1, collection2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullCollectionVsEmptyCollection_TrueReturned()
    {
        int[] collection1 = Array.Empty<int>();
        int[]? collection2 = null;

        bool result1 = new CollectionEqualityComparer<int>().Equals(collection1, collection2);
        bool result2 = new CollectionEqualityComparer<int>().Equals(collection2, collection1);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
