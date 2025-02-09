// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
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
        MessageHeaderCollection headers = [];

        BinaryMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        string? typeHeaderValue = headers["x-message-type"];
        typeHeaderValue.ShouldNotBeNullOrEmpty();
        typeHeaderValue.ShouldStartWith("Silverback.Messaging.Messages.BinaryMessage, Silverback.Integration,");
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        BinaryMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageStream,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ShouldBeSameAs(messageStream);
    }

    [Fact]
    public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes("test");

        BinaryMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().ShouldBe(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_BinaryMessage_RawContentProduced()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = [];

        Stream? result = await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.ShouldBeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_InheritedBinaryMessage_RawContentProduced()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        MessageHeaderCollection headers = [];

        Stream? result = await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        result.ShouldBeSameAs(message.Content);
    }

    [Fact]
    public async Task SerializeAsync_NonBinaryMessage_ExceptionThrown()
    {
        TestEventOne message = new() { Content = "hey!" };
        MessageHeaderCollection headers = [];

        Func<Task> act = async () => await new BinaryMessageSerializer().SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        Stream? result = await new BinaryMessageSerializer().SerializeAsync(
            null,
            [],
            TestProducerEndpoint.GetDefault());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_NullMessageWithHardcodedType_NullReturned()
    {
        Stream? serialized = await new BinaryMessageSerializer()
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        BinaryMessageSerializer serializer1 = new();
        BinaryMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        BinaryMessageSerializer serializer1 = new();
        BinaryMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    private sealed class InheritedBinaryMessage : BinaryMessage;
}
