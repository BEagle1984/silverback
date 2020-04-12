// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class MessageHeaderCollectionTests
    {
        [Fact]
        public void IntIndexer_GetExistingIndex_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection[1].Should().BeEquivalentTo(new MessageHeader("two", "2"));
        }

        [Fact]
        public void StringIndexer_GetExistingKey_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection["two"].Should().BeEquivalentTo("2");
        }

        [Fact]
        public void StringIndexer_GetNonExistingKey_ExceptionThrown()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            Func<string> act = () => collection["four"];

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void StringIndexer_SetNonExistingKey_HeaderAdded()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection["four"] = "4";

            collection.Last().Should().BeEquivalentTo(new MessageHeader("four", "4"));
        }

        [Fact]
        public void StringIndexer_SetExistingKey_HeaderReplaced()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection["two"] = "2!";

            collection.GetValue("two").Should().Be("2!");
        }

        [Fact]
        public void Add_SomeHeaders_HeadersAdded()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.Should().BeEquivalentTo(
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3"));
        }

        [Fact]
        public void AddOrReplace_ExistingHeader_ValueReplaced()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" }
            };

            collection.AddOrReplace("one", "1(2)");


            collection.Should().BeEquivalentTo(
                new MessageHeader("one", "1(2)"),
                new MessageHeader("two", "2"));
        }

        [Fact]
        public void AddOrReplace_NewHeader_HeaderAdded()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" }
            };

            collection.AddOrReplace("three", "3");

            collection.Should().BeEquivalentTo(
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3"));
        }

        [Fact]
        public void Contains_ExistingKey_ReturnsTrue()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.Contains("one").Should().BeTrue();
        }

        [Fact]
        public void Contains_NonExistingKey_ReturnsFalse()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.Contains("four").Should().BeFalse();
        }

        [Fact]
        public void GetValue_ExistingKey_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValue("two").Should().BeEquivalentTo("2");
        }

        [Fact]
        public void GetValue_NonExistingKey_NullIsReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValue("four").Should().BeEquivalentTo(null);
        }

        [Fact]
        public void TypedGetValue_ExistingKey_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValue<int>("two").Should().Be(2);
        }

        [Fact]
        public void TypedGetValue_NonExistingKey_NullIsReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValue<int>("four").Should().Be(null);
        }

        [Fact]
        public void GetValueOrDefault_ExistingKey_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValueOrDefault("two", typeof(int)).Should().Be(2);
        }

        [Fact]
        public void GetValueOrDefault_NonExistingKey_DefaultValueIsReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValueOrDefault("four", typeof(int)).Should().Be(0);
        }

        [Fact]
        public void TypedGetValueOrDefault_ExistingKey_HeaderReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValueOrDefault<int>("two").Should().Be(2);
        }

        [Fact]
        public void TypedGetValueOrDefault_NonExistingKey_DefaultValueIsReturned()
        {
            var collection = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" },
                { "three", "3" }
            };

            collection.GetValueOrDefault<int>("four").Should().Be(0);
        }
    }
}