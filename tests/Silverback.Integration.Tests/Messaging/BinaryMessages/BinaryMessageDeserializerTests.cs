// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageDeserializerTests
{
    [Fact]
    public async Task DeserializeAsync_ShouldReturnBinaryMessage()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = [];

        BinaryMessageSerializer serializer = new();
        BinaryMessageDeserializer<BinaryMessage> deserializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnCustomModel()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = [];

        BinaryMessageSerializer serializer = new();
        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnBinaryMessageFromStream()
    {
        MemoryStream rawContent = new([0x01, 0x02, 0x03, 0x04, 0x05]);
        MessageHeaderCollection headers = [];

        (object? deserialized, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new BinaryMessage
            {
                Content = rawContent
            });
        type.ShouldBe(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnCustomModelFromTypeHeader()
    {
        MemoryStream rawContent = new([0x01, 0x02, 0x03, 0x04, 0x05]);
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(InheritedBinaryMessage).AssemblyQualifiedName! }
        };

        (object? deserialized, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.ShouldBe(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnCustomModelFromHardcodedType()
    {
        MemoryStream rawContent = new([0x01, 0x02, 0x03, 0x04, 0x05]);
        MessageHeaderCollection headers = [];

        (object? deserialized, Type type) = await new BinaryMessageDeserializer<InheritedBinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.ShouldBe(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnBinaryWithNullContent()
    {
        (object? deserialized, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(null, [], TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new BinaryMessage
            {
                Content = null
            });
        type.ShouldBe(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnBinaryWithEmptyContent()
    {
        (object? deserialized, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(
                new MemoryStream(),
                [],
                TestConsumerEndpoint.GetDefault());

        BinaryMessage deserializedBinaryMessage = deserialized.ShouldBeOfType<BinaryMessage>();
        deserializedBinaryMessage.Content.ReadAll()!.Length.ShouldBe(0);
        type.ShouldBe(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnCustomModelFromStreamAndTypeHeader()
    {
        MemoryStream rawContent = new([0x01, 0x02, 0x03, 0x04, 0x05]);
        MessageHeaderCollection headers = [];

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserialized, Type type) = await deserializer.DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.ShouldBe(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnCustomModelFromStreamAndHardcodedType()
    {
        MessageHeaderCollection headers = [];

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserialized, Type type) = await deserializer.DeserializeAsync(null, headers, TestConsumerEndpoint.GetDefault());

        deserialized.ShouldBeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = null
            });
        type.ShouldBe(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnEmptyCustomModel()
    {
        MessageHeaderCollection headers = [];

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserialized, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            headers,
            TestConsumerEndpoint.GetDefault());

        BinaryMessage deserializedBinaryMessage = deserialized.ShouldBeOfType<InheritedBinaryMessage>();
        deserializedBinaryMessage.Content.ReadAll()!.Length.ShouldBe(0);
        type.ShouldBe(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldThrow_WhenBadTypeHeader()
    {
        Stream rawContent = BytesUtil.GetRandomStream();
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Bad.TestEventOne, Silverback.Integration.Tests"
            }
        };
        BinaryMessageDeserializer<BinaryMessage> serializer = new();

        Func<Task> act = async () => await serializer.DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        await act.ShouldThrowAsync<TypeLoadException>();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        BinaryMessageDeserializer<BinaryMessage> deserializer1 = new();
        BinaryMessageDeserializer<BinaryMessage> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        BinaryMessageDeserializer<BinaryMessage> deserializer1 = new();
        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    private sealed class InheritedBinaryMessage : BinaryMessage;
}
