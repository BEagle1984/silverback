// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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

public class JsonMessageDeserializerFixture
{
    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeSerializedMessage()
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
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new();

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Ignore)]
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader_WhenNotMandatory(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new();

        JsonMessageDeserializer<TestEventOne> deserializer = new()
        {
            TypeHeaderBehavior = typeHeaderBehavior
        };

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserializeChildType()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Mandatory)]
    public async Task DeserializeAsync_ShouldDeserializeChildType_WhenTypeHeaderBehaviorNotIgnore(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<IEvent> deserializer = new()
        {
            TypeHeaderBehavior = typeHeaderBehavior
        };

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Mandatory)]
    public async Task DeserializeAsync_ShouldDeserializeRegardlessOfBaseType_WhenTypeHeaderBehaviorNotIgnore(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<TestEventTwo> deserializer = new()
        {
            TypeHeaderBehavior = typeHeaderBehavior
        };

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldIgnoreTypeHeader_WhenBehaviorSetToIgnore()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventTwo).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new()
        {
            TypeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Ignore
        };

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldIgnoreWrongTypeHeader_WhenBehaviorSetToIgnore()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", "What.The.Header" }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new()
        {
            TypeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Ignore
        };

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeOfType<TestEventOne>();
        deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldThrowWhenMissingTypeHeader_WhenBehaviorSetToMandatory()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new();

        JsonMessageDeserializer<TestEventOne> deserializer = new()
        {
            TypeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Mandatory
        };

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Message type header *");
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Mandatory)]
    public async Task DeserializeAsync_ShouldThrowWhenWrongTypeHeader_WhenBehaviorNotIgnore(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", "What.The.Header" }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new()
        {
            TypeHeaderBehavior = typeHeaderBehavior
        };

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        await act.Should().ThrowAsync<TypeLoadException>();
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(null, new MessageHeaderCollection(), TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenEmptyStream()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        deserializedObject.Should().BeNull();
        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameSettings()
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
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        JsonMessageDeserializer<TestEventTwo> deserializer2 = new()
        {
            Options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            }
        };

        bool result = Equals(deserializer1, deserializer2);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentSettings()
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
