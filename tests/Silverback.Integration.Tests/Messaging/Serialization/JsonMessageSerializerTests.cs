// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    [Fact]
    public async Task SerializeAsync_WithDefaultSettings_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_ChildType_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<object> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_TypeImplementingInterface_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<IEvent> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<TestEventOne> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ChildType_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<object> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_TypeImplementingInterface_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<IEvent> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<TestEventOne> serializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).Should().NotContain("TestEventOne");

        serialized.Position = 0;

        (object? deserialized, _) = await serializer
            .DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne? message2 = deserialized as TestEventOne;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));

        JsonMessageSerializer<TestEventOne> serializer = new();

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

        JsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<TestEventOne> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_TypeReturned()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        JsonMessageSerializer<TestEventOne> serializer = new();

        (_, Type type) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_WithTypeHeader_ChildTypeDeserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        JsonMessageSerializer<IEvent> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_NullObjectReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_TypeReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer = new();

        (_, Type type) = await serializer
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(
                new MemoryStream(),
                new MessageHeaderCollection(),
                TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_TypeReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer = new();

        (_, Type type) = await serializer
            .DeserializeAsync(
                new MemoryStream(),
                new MessageHeaderCollection(),
                TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer1 = new();
        JsonMessageSerializer<TestEventOne> serializer2 = serializer1;

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageSerializer<TestEventOne> serializer2 = new()
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
        JsonMessageSerializer<TestEventOne> serializer1 = new();
        JsonMessageSerializer<TestEventOne> serializer2 = new();

        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
    {
        JsonMessageSerializer<TestEventOne> serializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageSerializer<TestEventOne> serializer2 = new()
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
