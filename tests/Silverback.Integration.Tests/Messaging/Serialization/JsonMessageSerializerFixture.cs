// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class JsonMessageSerializerFixture
{
    [Fact]
    public async Task SerializeAsync_ShouldSerialize()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = "{\"Content\":\"the message\"}"u8.ToArray();
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_ShouldAddTypeHeader()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ShouldNotAddTypeHeader_WhenDisabled()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new()
        {
            MustSetTypeHeader = false
        };

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnUnmodifiedStream()
    {
        MemoryStream messageStream = new("test"u8.ToArray());

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageStream,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.Should().BeSameAs(messageStream);
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnUnmodifiedByteArray()
    {
        byte[] messageBytes = "test"u8.ToArray();

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameSettings()
    {
        JsonMessageSerializer serializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageSerializer serializer2 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentSettings()
    {
        JsonMessageSerializer serializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageSerializer serializer2 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = false
            }
        };

        bool result = Equals(serializer1, serializer2);

        result.Should().BeFalse();
    }
}
