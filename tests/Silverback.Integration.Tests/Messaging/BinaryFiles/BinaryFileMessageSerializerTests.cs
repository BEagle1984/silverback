// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles;

public class BinaryFileMessageSerializerTests
{
    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        BinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await serializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryFileMessage? message2 = deserialized as BinaryFileMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeDeserializeAsync_HardcodedType_CorrectlyDeserialized()
    {
        InheritedBinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
        (object? deserialized, _) = await serializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        BinaryFileMessage? message2 = deserialized as BinaryFileMessage;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        BinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        string? typeHeaderValue = headers["x-message-type"];
        typeHeaderValue.Should().NotBeNullOrEmpty();
        typeHeaderValue.Should().StartWith("Silverback.Messaging.Messages.BinaryFileMessage, Silverback.Integration,");
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        BinaryFileMessageSerializer serializer = new();

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

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

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

        BinaryFileMessageSerializer serializer = new();

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

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_BinaryFileMessage_RawContentProduced()
    {
        BinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        Stream? result = await new BinaryFileMessageSerializer()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_InheritedBinaryFileMessage_RawContentProduced()
    {
        InheritedBinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        Stream? result = await new BinaryFileMessageSerializer()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_NonBinaryFileMessage_ExceptionThrown()
    {
        TestEventOne message = new() { Content = "hey!" };
        MessageHeaderCollection headers = new();

        Func<Task> act = async () => await new BinaryFileMessageSerializer()
            .SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        Stream? result = await new BinaryFileMessageSerializer().SerializeAsync(
            null,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_NullMessageWithHardcodedType_NullReturned()
    {
        Stream? serialized = await new BinaryFileMessageSerializer()
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_Stream_BinaryFileReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        (object? deserializedObject, Type type) = await new BinaryFileMessageSerializer()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new BinaryFileMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(BinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(InheritedBinaryFileMessage).AssemblyQualifiedName! }
        };

        (object? deserializedObject, Type type) = await new BinaryFileMessageSerializer()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryFileMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        (object? deserializedObject, Type type) = await new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryFileMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_BinaryFileWithNullContentReturned()
    {
        (object? deserializedObject, Type type) = await new BinaryFileMessageSerializer()
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new BinaryFileMessage
            {
                Content = null
            });
        type.Should().Be(typeof(BinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyMessage_BinaryFileWithEmptyContentReturned()
    {
        (object? deserializedObject, Type type) = await new BinaryFileMessageSerializer()
            .DeserializeAsync(
                new MemoryStream(),
                new MessageHeaderCollection(),
                TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<BinaryFileMessage>();
        deserializedObject.As<BinaryFileMessage>().Content.ReadAll()!.Length.Should().Be(0);
        type.Should().Be(typeof(BinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_WithHardcodedType_CustomBinaryFileReturned()
    {
        MemoryStream rawContent = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
            .DeserializeAsync(rawContent, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryFileMessage
            {
                Content = rawContent
            });
        type.Should().Be(typeof(InheritedBinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessageWithHardcodedType_CustomBinaryFileReturned()
    {
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
            .DeserializeAsync(null, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
        deserializedObject.Should().BeEquivalentTo(
            new InheritedBinaryFileMessage
            {
                Content = null
            });
        type.Should().Be(typeof(InheritedBinaryFileMessage));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyMessageWithHardcodedType_CustomBinaryFileReturned()
    {
        MessageHeaderCollection headers = new();

        BinaryFileMessageSerializer<InheritedBinaryFileMessage> serializer = new();

        (object? deserializedObject, Type type) = await serializer
            .DeserializeAsync(new MemoryStream(), headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
        deserializedObject.As<BinaryFileMessage>().Content.ReadAll()!.Length.Should().Be(0);
        type.Should().Be(typeof(InheritedBinaryFileMessage));
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

    private sealed class InheritedBinaryFileMessage : BinaryFileMessage
    {
    }
}
