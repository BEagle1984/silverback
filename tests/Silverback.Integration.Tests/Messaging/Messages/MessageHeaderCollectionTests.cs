// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class MessageHeaderCollectionTests
    {
        [Fact]
        public void Add_SomeHeaders_HeadersAdded()
        {
            var collection = new MessageHeaderCollection
            {
                {"one", "1"},
                {"two", "2"},
                {"three", "3"}
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
                {"one", "1"}, 
                {"two", "2"}
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
                {"one", "1"}, 
                {"two", "2"}
            };

            collection.AddOrReplace("three", "3");

            collection.Should().BeEquivalentTo(
                new MessageHeader("one", "1"),
                new MessageHeader("two", "2"),
                new MessageHeader("three", "3"));
        }
    }
}