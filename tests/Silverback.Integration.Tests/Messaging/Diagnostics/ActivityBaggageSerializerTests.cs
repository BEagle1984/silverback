// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityBaggageSerializerTests
{
    [Fact]
    public void Serialize_ShouldSerializeToString()
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
    public void TryDeserialize_ShouldDeserialize()
    {
        string value = "key1=value1, key2 = value2, invalidPair,=valueonly,keyonly=,,=,";

        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize(value);

        result.ShouldContain(new KeyValuePair<string, string>("key1", "value1"));
        result.ShouldContain(new KeyValuePair<string, string>("key2", "value2"));
        result.ShouldContain(new KeyValuePair<string, string>("keyonly", ""));
        result.ShouldContain(new KeyValuePair<string, string>("", "valueonly"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(",")]
    [InlineData("(null)")]
    [InlineData("a=b=c")]
    [InlineData(",,,")]
    [InlineData("keyonly")]
    public void TryDeserialize_ShouldReturnEmptyCollection_WhenInputNotValid(string? deserializeValue)
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize(deserializeValue!);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Serialize_ShouldReturnEmptyString_WhenBaggageIsEmpty()
    {
        string result = ActivityBaggageSerializer.Serialize([]);

        result.ShouldBe("");
    }

    [Fact]
    public void Serialize_ShouldSerializeSingleItem()
    {
        string result = ActivityBaggageSerializer.Serialize([new KeyValuePair<string, string?>("key1", "value1")]);

        result.ShouldBe("key1=value1");
    }

    [Fact]
    public void Serialize_ShouldSerializeNullAndEmptyValues()
    {
        List<KeyValuePair<string, string?>> itemsToAdd =
        [
            new("key1", null),
            new("key2", ""),
            new("key3", "value3")
        ];

        string result = ActivityBaggageSerializer.Serialize(itemsToAdd);

        result.ShouldBe("key1=,key2=,key3=value3");
    }

    [Fact]
    public void Serialize_ShouldSerializeLongBaggage()
    {
        List<KeyValuePair<string, string?>> itemsToAdd =
        [
            new("key1", new string('a', 1000)),
            new("key2", new string('b', 1000))
        ];

        string result = ActivityBaggageSerializer.Serialize(itemsToAdd);

        result.ShouldBe($"key1={new string('a', 1000)},key2={new string('b', 1000)}");
    }

    [Fact]
    public void Deserialize_ShouldPreserveOrderAndDuplicates()
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result =
            ActivityBaggageSerializer.Deserialize("key2=value2,key1=value1,key2=value3");

        result.ShouldBe(
        [
            new KeyValuePair<string, string>("key2", "value2"),
            new KeyValuePair<string, string>("key1", "value1"),
            new KeyValuePair<string, string>("key2", "value3")
        ]);
    }

    [Fact]
    public void Deserialize_ShouldTrimKeysAndValues()
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result =
            ActivityBaggageSerializer.Deserialize(" key1 = value 1 ,\tkey2\t=\tvalue2\t");

        result.ShouldBe(
        [
            new KeyValuePair<string, string>("key1", "value 1"),
            new KeyValuePair<string, string>("key2", "value2")
        ]);
    }

    [Fact]
    public void Deserialize_ShouldSkipItemsWithMultipleKeyValueSeparators()
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize("key1=a=b,key2=value2,=,key3==");

        result.ShouldBe(
        [
            new KeyValuePair<string, string>("key2", "value2"),
            new KeyValuePair<string, string>("", "")
        ]);
    }

    [Fact]
    public void Deserialize_ShouldSplitValuesContainingItemSeparator()
    {
        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize("key1=va,lue1,key2=value2");

        result.ShouldBe(
        [
            new KeyValuePair<string, string>("key1", "va"),
            new KeyValuePair<string, string>("key2", "value2")
        ]);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldRoundTrip()
    {
        List<KeyValuePair<string, string?>> items =
        [
            new("key1", "value1"),
            new("key2", ""),
            new("key3", "value3"),
            new("key1", "value4")
        ];

        IReadOnlyCollection<KeyValuePair<string, string>> result =
            ActivityBaggageSerializer.Deserialize(ActivityBaggageSerializer.Serialize(items));

        result.ShouldBe(
        [
            new KeyValuePair<string, string>("key1", "value1"),
            new KeyValuePair<string, string>("key2", ""),
            new KeyValuePair<string, string>("key3", "value3"),
            new KeyValuePair<string, string>("key1", "value4")
        ]);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldRoundTripSingleItem()
    {
        string serialized = ActivityBaggageSerializer.Serialize([new KeyValuePair<string, string?>("key1", "value1")]);

        IReadOnlyCollection<KeyValuePair<string, string>> result = ActivityBaggageSerializer.Deserialize(serialized);

        result.ShouldBe([new KeyValuePair<string, string>("key1", "value1")]);
    }
}
