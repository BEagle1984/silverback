// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class BaggageConverterTests
    {
        [Fact]
        public void Serialize_SomeItems_ReturnsSerializedString()
        {
            var itemsToAdd = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3")
            };

            var result = BaggageConverter.Serialize(itemsToAdd);

            result.Should().Be("key1=value1,key2=value2,key3=value3");
        }

        [Fact]
        public void TryDeserialize_ValidString_CorrectlyDeserializesItems()
        {
            var value = "key1=value1, key2 = value2, invalidPair";

            var deserialized = BaggageConverter.TryDeserialize(value, out var result);

            deserialized.Should().BeTrue();
            result.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key1", "value1"));
            result.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key2", "value2"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryDeserialize_NullOrEmpty_ReturnsFalse(string deserializeValue)
        {
            var deserialized = BaggageConverter.TryDeserialize(deserializeValue, out var result);

            deserialized.Should().BeFalse();
            result.Should().BeNull();
        }
    }
}
