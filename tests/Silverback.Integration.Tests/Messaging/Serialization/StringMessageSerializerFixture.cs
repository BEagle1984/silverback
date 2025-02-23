// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class StringMessageSerializerFixture
{
    [Fact]
    public async Task SerializeAsync_ShouldSerialize()
    {
        StringMessage message = new StringMessage<TestEventOne>("the message");
        MessageHeaderCollection headers = [];

        StringMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = "the message"u8.ToArray();
        serialized.ReadAll().ShouldBe(expected);
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        StringMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenContentIsNull()
    {
        StringMessage message = new StringMessage<TestEventOne>(null);
        MessageHeaderCollection headers = [];

        StringMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldThrow_WhenMessageIsNotStringMessage()
    {
        StringMessageSerializer serializer = new();

        Func<Task> act = () => serializer
            .SerializeAsync(new TestEventOne(), [], TestProducerEndpoint.GetDefault()).AsTask();

        Exception exception = await act.ShouldThrowAsync<ArgumentException>();
        exception.Message.ShouldMatch("The message must be a StringMessage. .*");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        StringMessageSerializer serializer1 = new();
        StringMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameEncoding()
    {
        StringMessageSerializer serializer1 = new(MessageEncoding.UTF8);
        StringMessageSerializer serializer2 = new(MessageEncoding.UTF8);

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        StringMessageSerializer serializer1 = new();
        StringMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentEncoding()
    {
        StringMessageSerializer serializer1 = new(MessageEncoding.UTF8);
        StringMessageSerializer serializer2 = new(MessageEncoding.ASCII);

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeFalse();
    }
}
