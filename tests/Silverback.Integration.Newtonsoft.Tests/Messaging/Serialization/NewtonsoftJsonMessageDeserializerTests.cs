// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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

public class NewtonsoftJsonMessageDeserializerTests
{
    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer serializer = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).Should().NotContain("TestEventOne");

        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne? message2 = deserialized as TestEventOne;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

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

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

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

        NewtonsoftJsonMessageDeserializer<IEvent> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_WrongTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_WrongTypeHeader_TypeReturned()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_NullObjectReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(
            null,
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_TypeReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(
            null,
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_TypeReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_DifferentType_FalseReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageDeserializer<TestEventTwo> deserializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DefaultSettings_TrueReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeFalse();
    }
}
