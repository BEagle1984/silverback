// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class RawMessageDeserializerFixture
{
    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeTypedRawMessage()
    {
        Stream content = new MemoryStream([0x01, 0x02, 0x03]);
        MessageHeaderCollection headers = [];
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer = new();

        (object? deserialized, _) = await deserializer.DeserializeAsync(content, headers, TestConsumerEndpoint.GetDefault());

        RawMessage<TestEventOne> rawMessage = deserialized.ShouldBeOfType<RawMessage<TestEventOne>>();
        rawMessage.Content.ShouldBe(content);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeBaseRawMessage()
    {
        Stream content = new MemoryStream([0x01, 0x02, 0x03]);
        MessageHeaderCollection headers = [];
        RawMessageDeserializer<RawMessage> deserializer = new();

        (object? deserialized, _) = await deserializer.DeserializeAsync(content, headers, TestConsumerEndpoint.GetDefault());

        RawMessage rawMessage = deserialized.ShouldBeOfType<RawMessage>();
        rawMessage.Content.ShouldBe(content);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNullContent_WhenMessageIsNull()
    {
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer = new();

        (object? deserialized, Type _) = await deserializer.DeserializeAsync(null, [], TestConsumerEndpoint.GetDefault());

        RawMessage<TestEventOne> rawMessage = deserialized.ShouldBeOfType<RawMessage<TestEventOne>>();
        rawMessage.Content.ShouldBeNull();
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNullContent_WhenEmptyStream()
    {
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer = new();

        (object? deserialized, Type _) = await deserializer.DeserializeAsync(new MemoryStream(), [], TestConsumerEndpoint.GetDefault());

        RawMessage<TestEventOne> rawMessage = deserialized.ShouldBeOfType<RawMessage<TestEventOne>>();
        rawMessage.Content.ShouldBeNull();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer1 = new();
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameType()
    {
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer1 = new();
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        RawMessageDeserializer<RawMessage<TestEventOne>> deserializer1 = new();
        RawMessageDeserializer<RawMessage<TestEventTwo>> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }
}
