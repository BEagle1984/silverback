// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class StringMessageDeserializerFixture
{
    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeTypedStringMessage()
    {
        StringMessage message = new StringMessage<TestEventOne>("the message");
        MessageHeaderCollection headers = [];

        StringMessageSerializer serializer = new();
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).ShouldNotContain("TestEventOne");

        serialized.Position = 0;

        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        StringMessage<TestEventOne> stringMessage = deserialized.ShouldBeOfType<StringMessage<TestEventOne>>();
        stringMessage.Content.ShouldBe(message.Content);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeBaseStringMessage()
    {
        StringMessage message = new StringMessage<TestEventOne>("the message");
        MessageHeaderCollection headers = [];

        StringMessageSerializer serializer = new();
        StringMessageDeserializer<StringMessage> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).ShouldNotContain("TestEventOne");

        serialized.Position = 0;

        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        StringMessage stringMessage = deserialized.ShouldBeOfType<StringMessage>();
        stringMessage.Content.ShouldBe(message.Content);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNullContent_WhenMessageIsNull()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer = new();

        (object? deserialized, Type _) = await deserializer.DeserializeAsync(null, [], TestConsumerEndpoint.GetDefault());

        StringMessage<TestEventOne> stringMessage = deserialized.ShouldBeOfType<StringMessage<TestEventOne>>();
        stringMessage.Content.ShouldBeNull();
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNullContent_WhenEmptyStream()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer = new();

        (object? deserialized, Type _) = await deserializer.DeserializeAsync(new MemoryStream(), [], TestConsumerEndpoint.GetDefault());

        StringMessage<TestEventOne> stringMessage = deserialized.ShouldBeOfType<StringMessage<TestEventOne>>();
        stringMessage.Content.ShouldBeNull();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer1 = new();
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameEncoding()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer1 = new(MessageEncoding.UTF8);
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer2 = new(MessageEncoding.UTF8);

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer1 = new();
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer1 = new(MessageEncoding.UTF8);
        StringMessageDeserializer<StringMessage<TestEventTwo>> deserializer2 = new(MessageEncoding.UTF8);

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentEncoding()
    {
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer1 = new(MessageEncoding.UTF8);
        StringMessageDeserializer<StringMessage<TestEventOne>> deserializer2 = new(MessageEncoding.ASCII);

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }
}
