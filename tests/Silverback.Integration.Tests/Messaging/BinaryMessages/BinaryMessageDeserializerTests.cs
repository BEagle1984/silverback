// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageDeserializerTests
{
    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer serializer = new();
        BinaryMessageDeserializer<BinaryMessage> deserializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryMessage? message2 = deserialized as BinaryMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeDeserializeAsync_CustomModel_CorrectlyDeserialized()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer serializer = new();
        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryMessage? message2 = deserialized as BinaryMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_Stream_BinaryReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        (object? deserializedObject, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new BinaryMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ByteArrayWithTypeHeader_CustomBinaryReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(InheritedBinaryMessage).AssemblyQualifiedName! }
        };

        (object? deserializedObject, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ByteArrayWithHardcodedType_CustomBinaryReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        (object? deserializedObject, Type type) = await new BinaryMessageDeserializer<InheritedBinaryMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_BinaryWithNullContentReturned()
    {
        (object? deserializedObject, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new BinaryMessage
            {
                Content = null
            });
        type.Should().Be(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyMessage_BinaryWithEmptyContentReturned()
    {
        (object? deserializedObject, Type type) = await new BinaryMessageDeserializer<BinaryMessage>()
            .DeserializeAsync(
                new MemoryStream(),
                new MessageHeaderCollection(),
                TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryMessage>();
        deserializedObject.As<BinaryMessage>().Content.ReadAll()!.Length.Should().Be(0);
        type.Should().Be(typeof(BinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_WithHardcodedType_CustomBinaryReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessageWithHardcodedType_CustomBinaryReturned()
    {
        MessageHeaderCollection headers = new();

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(null, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryMessage
            {
                Content = null
            });
        type.Should().Be(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyMessageWithHardcodedType_CustomBinaryReturned()
    {
        MessageHeaderCollection headers = new();

        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            headers,
            TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryMessage>();
        deserializedObject.As<BinaryMessage>().Content.ReadAll()!.Length.Should().Be(0);
        type.Should().Be(typeof(InheritedBinaryMessage));
    }

    [Fact]
    public async Task DeserializeAsync_BadTypeHeader_ExceptionThrown()
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

        await act.Should().ThrowAsync<TypeLoadException>();
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        BinaryMessageDeserializer<BinaryMessage> deserializer1 = new();
        BinaryMessageDeserializer<BinaryMessage> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_DifferentType_FalseReturned()
    {
        BinaryMessageDeserializer<BinaryMessage> deserializer1 = new();
        BinaryMessageDeserializer<InheritedBinaryMessage> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeFalse();
    }

    private sealed class InheritedBinaryMessage : BinaryMessage
    {
    }
}
