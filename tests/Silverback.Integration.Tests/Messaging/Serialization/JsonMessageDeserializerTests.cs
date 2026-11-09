// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class JsonMessageDeserializerTests
{
    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeSerializedMessage()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        JsonMessageSerializer serializer = new();
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).ShouldNotContain("TestEventOne");

        serialized.Position = 0;

        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne message2 = deserialized.ShouldBeOfType<TestEventOne>();
        message2.ShouldBeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = [];

        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Ignore)]
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader_WhenNotMandatory(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = [];

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Mandatory)]
    public async Task DeserializeAsync_ShouldDeserializeChildType_WhenObservingMessageTypeHeader(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
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

        JsonMessageDeserializer<IEvent> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
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

        JsonMessageDeserializer<TestEventTwo> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldIgnoreTypeHeader_WhenBehaviorSetToIgnore()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventTwo).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Ignore);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldIgnoreWrongTypeHeader_WhenBehaviorSetToIgnore()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", "What.The.Header" }
        };

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Ignore);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldThrowWhenMissingTypeHeader_WhenBehaviorSetToMandatory()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = [];

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Mandatory);

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        Exception exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldMatch("Message type header .*");
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

        JsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        await act.ShouldThrowAsync<TypeLoadException>();
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserialize_WhenUsingSourceGeneratedContext()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());

        JsonMessageDeserializer<TestEventOne> deserializer = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventOneJsonSerializerContext.Default
            });

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, [], TestConsumerEndpoint.GetDefault());

        TestEventOne testEventOne = deserializedObject.ShouldBeOfType<TestEventOne>();
        testEventOne.Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Theory]
    [InlineData(typeof(TestEventOne))]
    [InlineData(typeof(TestEventTwo))]
    public async Task DeserializeAsync_ShouldDeserializeTypeFromHeader_WhenCombiningSourceGeneratedContexts(Type messageType)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", messageType.AssemblyQualifiedName }
        };

        JsonSerializerOptions options = new();
        options.TypeInfoResolverChain.Add(TestEventOneJsonSerializerContext.Default);
        options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default);
        JsonMessageDeserializer<object> deserializer = new(options);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.GetType().ShouldBe(messageType);
        string? content = deserializedObject switch
        {
            TestEventOne testEventOne => testEventOne.Content,
            TestEventTwo testEventTwo => testEventTwo.Content,
            _ => null
        };
        content.ShouldBe("the message");
        type.ShouldBe(messageType);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldThrow_WhenTypeNotInSourceGeneratedContext()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventTwo).AssemblyQualifiedName }
        };

        JsonMessageDeserializer<object> deserializer = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventOneJsonSerializerContext.Default
            });

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        Exception exception = await act.ShouldThrowAsync<NotSupportedException>();
        exception.Message.ShouldContain(nameof(TestEventTwo));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(null, [], TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldBeNull();
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenEmptyStream()
    {
        JsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            [],
            TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldBeNull();
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameSettings()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        JsonMessageDeserializer<TestEventOne> deserializer2 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new();
        JsonMessageDeserializer<TestEventOne> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        JsonMessageDeserializer<TestEventTwo> deserializer2 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentSettings()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 42
            });

        JsonMessageDeserializer<TestEventOne> deserializer2 = new(
            new JsonSerializerOptions
            {
                AllowTrailingCommas = false
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Equals_ShouldReturnTrue_WhenSameSettingsAndOneDeserializerHasBeenUsed()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(new JsonSerializerOptions { MaxDepth = 42 });
        JsonMessageDeserializer<TestEventOne> deserializer2 = new(new JsonSerializerOptions { MaxDepth = 42 });

        await deserializer1.DeserializeAsync(
            new MemoryStream("{\"Content\":\"the message\"}"u8.ToArray()),
            [],
            TestConsumerEndpoint.GetDefault());

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentTypeInfoResolver()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventOneJsonSerializerContext.Default
            });

        JsonMessageDeserializer<TestEventOne> deserializer2 = new(
            new JsonSerializerOptions
            {
                TypeInfoResolver = TestEventTwoJsonSerializerContext.Default
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentTypeHeaderBehavior()
    {
        JsonMessageDeserializer<TestEventOne> deserializer1 = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Optional);
        JsonMessageDeserializer<TestEventOne> deserializer2 = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Ignore);

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }
}
