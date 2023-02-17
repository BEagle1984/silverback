// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
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

public class JsonMessageSerializerTests
{
    // TODO: Test with or without type header + options like IgnoreTypeHeader


    [Fact]
    public async Task SerializeAsync_WithDefaultSettings_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        JsonMessageSerializer serializer = new();

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

        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        JsonMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
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
    public void Equals_DefaultSettings_TrueReturned()
    {
        JsonMessageSerializer serializer1 = new();
        JsonMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
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
