// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageSerializerTests
{
    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        BinaryMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        string? typeHeaderValue = headers["x-message-type"];
        typeHeaderValue.Should().NotBeNullOrEmpty();
        typeHeaderValue.Should().StartWith("Silverback.Messaging.Messages.BinaryMessage, Silverback.Integration,");
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        BinaryMessageSerializer serializer = new();

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

        BinaryMessageSerializer serializer = new();

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

        Stream? result = await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_InheritedBinaryMessage_RawContentProduced()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = new();

        Stream? result = await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_NonBinaryMessage_ExceptionThrown()
    {
        TestEventOne message = new() { Content = "hey!" };
        MessageHeaderCollection headers = new();

        Func<Task> act = async () => await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        Stream? result = await new BinaryMessageSerializer().SerializeAsync(
            null,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_NullMessageWithHardcodedType_NullReturned()
    {
        Stream? serialized = await new BinaryMessageSerializer()
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        BinaryMessageSerializer serializer1 = new();
        BinaryMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        BinaryMessageSerializer serializer1 = new();
        BinaryMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    private sealed class InheritedBinaryMessage : BinaryMessage
    {
    }
}
