// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivityBaggageSerializerTests
    {
        [Fact]
        public void Serialize_SomeItems_SerializedStringReturned()
        {
            var itemsToAdd = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3")
            };

            var result = ActivityBaggageSerializer.Serialize(itemsToAdd);

            result.Should().Be("key1=value1,key2=value2,key3=value3");
        }

        [Fact]
        public void TryDeserialize_ValidString_CorrectlyDeserializedItems()
        {
            var value = "key1=value1, key2 = value2, invalidPair";

            var result = ActivityBaggageSerializer.Deserialize(value);

            result.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key1", "value1"));
            result.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key2", "value2"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryDeserialize_NullOrEmpty_EmptyCollectionReturned(string deserializeValue)
        {
            var result = ActivityBaggageSerializer.Deserialize(deserializeValue);

            result.Should().BeEmpty();
        }
    }
}
