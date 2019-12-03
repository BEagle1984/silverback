// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class BaggageConverterTests
    {
        [Fact]
        public void Serialize_CorrectlySerializesItems()
        {
            IList<KeyValuePair<string, string>> itemsToAdd = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3")
            };

            string result = BaggageConverter.Serialize(itemsToAdd);

            Assert.Equal("key1=value1,key2=value2,key3=value3", result);
        }

        [Fact]
        public void TryDeserialize_CorrectlyDeserializesItems()
        {
            var value = "key1=value1, key2 = value2, invalidPair";

            bool deserialized = BaggageConverter.TryDeserialize(value, out var result);

            Assert.True(deserialized);
            Assert.Contains(result, pair => pair.Key == "key1" && pair.Value == "value1");
            Assert.Contains(result, pair => pair.Key == "key2" && pair.Value == "value2");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryDeserialize_DoesNotDeserialize_IfNullOrEmpty(string deserializeValue)
        {
            bool deserialized = BaggageConverter.TryDeserialize(deserializeValue, out var result);

            Assert.Null(result);
            Assert.False(deserialized);
        }
    }
}
