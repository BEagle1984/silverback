// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageSerializerTests
{
    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer<BinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await serializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryMessage? message2 = deserialized as BinaryMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeDeserializeAsync_HardcodedType_CorrectlyDeserialized()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await serializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryMessage? message2 = deserialized as BinaryMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer<BinaryMessage> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        string? typeHeaderValue = headers["x-message-type"];
        typeHeaderValue.Should().NotBeNullOrEmpty();
        typeHeaderValue.Should().StartWith("Silverback.Messaging.Messages.BinaryMessage, Silverback.Integration,");
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        BinaryMessageSerializer<BinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageStream,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.Should().BeSameAs(messageStream);
    }

    [Fact]
    public async Task SerializeAsync_StreamWithHardcodedType_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageStream,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.Should().BeSameAs(messageStream);
    }

    [Fact]
    public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes("test");

        BinaryMessageSerializer<BinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_ByteArrayWithHardcodedType_ReturnedUnmodified()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes("test");

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_BinaryMessage_RawContentProduced()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        Stream? result = await new BinaryMessageSerializer<BinaryMessage>()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_InheritedBinaryMessage_RawContentProduced()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        Stream? result = await new BinaryMessageSerializer<BinaryMessage>()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_NonBinaryMessage_ExceptionThrown()
    {
        TestEventOne message = new() { Content = "hey!" };
        MessageHeaderCollection headers = new();

        Func<Task> act = async () => await new BinaryMessageSerializer<BinaryMessage>()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        Stream? result = await new BinaryMessageSerializer<BinaryMessage>().SerializeAsync(
            null,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_NullMessageWithHardcodedType_NullReturned()
    {
        Stream? serialized = await new BinaryMessageSerializer<BinaryMessage>()
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_Stream_BinaryReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        (object? deserializedObject, Type type) = await new BinaryMessageSerializer<BinaryMessage>()
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

        (object? deserializedObject, Type type) = await new BinaryMessageSerializer<BinaryMessage>()
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

        (object? deserializedObject, Type type) = await new BinaryMessageSerializer<InheritedBinaryMessage>()
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
        (object? deserializedObject, Type type) = await new BinaryMessageSerializer<BinaryMessage>()
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
        (object? deserializedObject, Type type) = await new BinaryMessageSerializer<BinaryMessage>()
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

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
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
    public async Task DeserializeAsync_NullMessageWithHardcodedType_CustomBinaryReturned()
    {
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
            .DeserializeAsync(null, headers, TestConsumerEndpoint.GetDefault());

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

        BinaryMessageSerializer<InheritedBinaryMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
            .DeserializeAsync(new MemoryStream(), headers, TestConsumerEndpoint.GetDefault());

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
        JsonMessageSerializer<object> serializer = new();

        Func<Task> act = async () => await serializer
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        await act.Should().ThrowAsync<TypeLoadException>();
    }

    private sealed class InheritedBinaryMessage : BinaryMessage
    {
    }
}
