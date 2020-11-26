// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class CollectionEqualityComparerTests
    {
        [Fact]
        public void Equals_SamePrimitivesCollection_TrueIsReturned()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };

            var result = new CollectionEqualityComparer<int>().Equals(collection, collection);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SamePrimitives_TrueIsReturned()
        {
            var collection1 = new[] { 1, 2, 3, 4, 5 };
            var collection2 = new[] { 1, 2, 3, 4, 5 };

            var result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SamePrimitivesDifferentOrder_TrueIsReturned()
        {
            var collection1 = new[] { 1, 2, 3, 4, 5 };
            var collection2 = new[] { 1, 5, 3, 4, 2 };

            var result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentPrimitives_FalseIsReturned()
        {
            var collection1 = new[] { 1, 2, 3, 4, 5 };
            var collection2 = new[] { 1, 2, 4, 5, 6 };

            var result = new CollectionEqualityComparer<int>().Equals(collection1, collection2);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_SameObjectsCollection_TrueIsReturned()
        {
            var collection = new IEvent[]
            {
                new TestEventOne { Message = "one" },
                new TestEventOne { Message = "two" },
                new TestEventTwo()
            };

            var result = new CollectionEqualityComparer<IEvent>().Equals(collection, collection);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameObjects_TrueIsReturned()
        {
            var event1 = new TestEventOne { Message = "one" };
            var event2 = new TestEventOne { Message = "two" };
            var event3 = new TestEventOne { Message = "three" };

            var collection1 = new[]
            {
                event1,
                event2,
                event3
            };
            var collection2 = new[]
            {
                event1,
                event2,
                event3
            };

            var result =
                new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message).Equals(
                    collection1,
                    collection2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameItemsDifferentOrder_TrueIsReturned()
        {
            var event1 = new TestEventOne { Message = "one" };
            var event2 = new TestEventOne { Message = "two" };
            var event3 = new TestEventOne { Message = "three" };

            var collection1 = new[]
            {
                event1,
                event2,
                event3
            };
            var collection2 = new[]
            {
                event2,
                event1,
                event3
            };

            var result = new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message)
                .Equals(collection1, collection2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentObjects_FalseIsReturned()
        {
            var event1 = new TestEventOne { Message = "one" };
            var event2 = new TestEventOne { Message = "two" };
            var event3 = new TestEventOne { Message = "three" };

            var collection1 = new[]
            {
                event1,
                event3
            };
            var collection2 = new[]
            {
                event1,
                event2,
                event3
            };

            var result = new CollectionEqualityComparer<TestEventOne, string>(obj => obj.Message)
                .Equals(collection1, collection2);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_NullCollectionVsEmptyCollection_TrueIsReturned()
        {
            var collection1 = Array.Empty<int>();
            var collection2 = (int[]?)null;

            var result1 = new CollectionEqualityComparer<int>().Equals(collection1, collection2);
            var result2 = new CollectionEqualityComparer<int>().Equals(collection2, collection1);

            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }
    }
}
