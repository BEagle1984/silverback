// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityBaggageSerializerTests
{
    [Fact]
    public void Serialize_SomeItems_SerializedStringReturned()
    {
        List<KeyValuePair<string, string?>> itemsToAdd =
        [
            new("key1", "value1"),
            new("key2", "value2"),
            new("key3", "value3")
        ];

        string result = ActivityBaggageSerializer.Serialize(itemsToAdd);

        result.ShouldBe("key1=value1,key2=value2,key3=value3");
    }

    [Fact]
    public void TryDeserialize_ValidString_CorrectlyDeserializedItems()
    {
        string value = "key1=value1, key2 = value2, invalidPair,=valueonly,keyonly=,,=,";

        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize(value);

        result.ShouldContain(new KeyValuePair<string, string>("key1", "value1"));
        result.ShouldContain(new KeyValuePair<string, string>("key2", "value2"));
        result.ShouldContain(new KeyValuePair<string, string>("keyonly", string.Empty));
        result.ShouldContain(new KeyValuePair<string, string>(string.Empty, "valueonly"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(",")]
    [InlineData("(null)")]
    public void TryDeserialize_InvalidString_EmptyCollectionReturned(string? deserializeValue)
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize(deserializeValue!);

        result.ShouldBeEmpty();
    }
}
