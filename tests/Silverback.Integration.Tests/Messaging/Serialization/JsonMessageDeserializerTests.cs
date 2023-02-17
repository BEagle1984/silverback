// Copyright (c) 2023 Sergio Aquilini
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

public class JsonMessageDeserializerTests
{
    // TODO: Test with or without type header + options like IgnoreTypeHeader


    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        JsonMessageSerializer serializer = new();
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).Should().NotContain("TestEventOne");

        serialized.Position = 0;

        (object? deserialized, _) = await deserializer
            .DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne? message2 = deserialized as TestEventOne;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_TypeReturned()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

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

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_NullObjectReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_TypeReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_TypeReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageDeserializer<TestEventOne> deserializer2 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultSettings_TrueReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageDeserializer<TestEventOne> deserializer2 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = false
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeFalse();
    }
}
