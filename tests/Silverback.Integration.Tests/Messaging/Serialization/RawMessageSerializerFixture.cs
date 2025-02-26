// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class RawMessageSerializerFixture
{
    [Fact]
    public async Task SerializeAsync_ShouldSerialize()
    {
        RawMessage message = new RawMessage<TestEventOne>(new MemoryStream([0x01, 0x02, 0x03]));
        MessageHeaderCollection headers = [];

        RawMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        serialized.ReadAll().ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        RawMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenContentIsNull()
    {
        RawMessage message = new RawMessage<TestEventOne>(null);
        MessageHeaderCollection headers = [];

        RawMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        serialized.ShouldBeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldThrow_WhenMessageIsNotRawMessage()
    {
        RawMessageSerializer serializer = new();

        Func<Task> act = () => serializer
            .SerializeAsync(new TestEventOne(), [], TestProducerEndpoint.GetDefault()).AsTask();

        Exception exception = await act.ShouldThrowAsync<ArgumentException>();
        exception.Message.ShouldMatch("The message must be a RawMessage. .*");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        RawMessageSerializer serializer1 = new();
        RawMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameType()
    {
        RawMessageSerializer serializer1 = new();
        RawMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentTyype()
    {
        RawMessageSerializer serializer1 = new();
        StringMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.ShouldBeFalse();
    }
}
