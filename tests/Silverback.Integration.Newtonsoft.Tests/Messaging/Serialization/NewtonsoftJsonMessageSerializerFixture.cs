// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Serialization;

public class NewtonsoftJsonMessageSerializerFixture
{
    [Fact]
    public async Task SerializeAsync_ShouldSerialize()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_ShouldAddTypeHeader()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageSerializer serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ShouldNotAddTypeHeader_WhenDisabled()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageSerializer serializer = new()
        {
            MustSetTypeHeader = false
        };

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes("test");

        NewtonsoftJsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes("test"));

        NewtonsoftJsonMessageSerializer serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            stream,
            [],
            TestProducerEndpoint.GetDefault());

        serialized.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        NewtonsoftJsonMessageSerializer serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, [], TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        NewtonsoftJsonMessageSerializer serializer1 = new();
        NewtonsoftJsonMessageSerializer serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        NewtonsoftJsonMessageSerializer serializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageSerializer serializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultSettings_TrueReturned()
    {
        NewtonsoftJsonMessageSerializer serializer1 = new();
        NewtonsoftJsonMessageSerializer serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
    {
        NewtonsoftJsonMessageSerializer serializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageSerializer serializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            }
        };

        bool result = Equals(serializer1, serializer2);

        result.Should().BeFalse();
    }
}
