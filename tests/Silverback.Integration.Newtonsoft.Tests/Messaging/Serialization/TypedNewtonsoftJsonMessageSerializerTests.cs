// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

public class NewtonsoftJsonMessageSerializerTests
{
    [Fact]
    public async Task SerializeAsync_WithDefaultSettings_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_ChildType_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<object> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_TypeImplementingInterface_CorrectlySerialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<IEvent> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
        serialized.ReadAll().Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeAsync_Message_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_ChildType_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<object> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeAsync_TypeImplementingInterface_TypeHeaderAdded()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<IEvent> serializer = new();

        await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());

        headers.GetValue("x-message-type").Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).Should().NotContain("TestEventOne");

        (object? deserialized, _) = await serializer
            .DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne? message2 = deserialized as TestEventOne;

        message2.Should().NotBeNull();
        message2.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes("test");

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            messageBytes,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsync_Stream_ReturnedUnmodified()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes("test"));

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer.SerializeAsync(
            stream,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault());

        serialized.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task SerializeAsync_NullMessage_NullReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        Stream? serialized = await serializer
            .SerializeAsync(null, new MessageHeaderCollection(), TestProducerEndpoint.GetDefault());

        serialized.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_MissingTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

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

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

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

        NewtonsoftJsonMessageSerializer<IEvent> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_WrongTypeHeader_Deserialized()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
    }

    [Fact]
    public async Task DeserializeAsync_WrongTypeHeader_TypeReturned()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new();

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        (_, Type type) = await serializer
            .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_NullObjectReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        (object? deserializedObject, _) = await serializer
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_TypeReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        (_, Type type) = await serializer
            .DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

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
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

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
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new();

        // ReSharper disable once EqualExpressionComparison
        bool result = Equals(serializer, serializer);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameSettings_TrueReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        // ReSharper disable once EqualExpressionComparison
        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultSettings_TrueReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer1 = new();
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer2 = new();

        // ReSharper disable once EqualExpressionComparison
        bool result = Equals(serializer1, serializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentSettings_FalseReturned()
    {
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer1 = new()
        {
            Settings = new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            }
        };

        NewtonsoftJsonMessageSerializer<TestEventOne> serializer2 = new()
        {
            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            }
        };

        // ReSharper disable once EqualExpressionComparison
        bool result = Equals(serializer1, serializer2);

        result.Should().BeFalse();
    }
}
