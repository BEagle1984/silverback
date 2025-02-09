// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Serialization;

public class NewtonsoftJsonMessageDeserializerFixture
{
    [Fact]
    public async Task DeserializeAsync_ShouldCorrectlyDeserializeSerializedMessage()
    {
        TestEventOne message = new() { Content = "the message" };
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageSerializer serializer = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        Stream serialized = (await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault()))!;

        Encoding.UTF8.GetString(serialized.ReadAll()!).ShouldNotContain("TestEventOne");

        (object? deserialized, _) = await deserializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());

        TestEventOne? message2 = deserialized as TestEventOne;

        message2.ShouldNotBeNull();
        message2.ShouldBeEquivalentTo(message);
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
    }

    [Theory]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Optional)]
    [InlineData(JsonMessageDeserializerTypeHeaderBehavior.Ignore)]
    public async Task DeserializeAsync_ShouldDeserializeDespiteMissingTypeHeader_WhenNotMandatory(JsonMessageDeserializerTypeHeaderBehavior typeHeaderBehavior)
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldDeserializeChildType()
    {
        MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        MessageHeaderCollection headers = new()
        {
            { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
        };

        NewtonsoftJsonMessageDeserializer<IEvent> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
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

        NewtonsoftJsonMessageDeserializer<IEvent> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
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

        NewtonsoftJsonMessageDeserializer<TestEventTwo> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
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

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Ignore);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
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

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Ignore);

        (object? deserializedObject, Type type) = await deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldNotBeNull();
        deserializedObject.ShouldBeOfType<TestEventOne>();
        deserializedObject.ShouldBeOfType<TestEventOne>().Content.ShouldBe("the message");
        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldThrowWhenMissingTypeHeader_WhenBehaviorSetToMandatory()
    {
        MemoryStream rawMessage = new("{\"Content\":\"the message\"}"u8.ToArray());
        MessageHeaderCollection headers = [];

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: JsonMessageDeserializerTypeHeaderBehavior.Mandatory);

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

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new(typeHeaderBehavior: typeHeaderBehavior);

        Func<Task> act = () => deserializer.DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault()).AsTask();

        await act.ShouldThrowAsync<TypeLoadException>();
    }

    [Fact]
    public async Task DeserializeAsync_NullMessage_NullObjectReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(
            null,
            [],
            TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldBeNull();
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenMessageIsNull()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(
            null,
            [],
            TestConsumerEndpoint.GetDefault());

        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public async Task DeserializeAsync_ShouldReturnNull_WhenEmptyStream()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (object? deserializedObject, _) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            [],
            TestConsumerEndpoint.GetDefault());

        deserializedObject.ShouldBeNull();
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_TypeReturned()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer = new();

        (_, Type type) = await deserializer.DeserializeAsync(
            new MemoryStream(),
            [],
            TestConsumerEndpoint.GetDefault());

        type.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = deserializer1;

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameSettings()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            });

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new(
            new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenBothHaveDefaultSettings()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new();
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new();

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            });

        NewtonsoftJsonMessageDeserializer<TestEventTwo> deserializer2 = new(
            new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentSettings()
    {
        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer1 = new(
            new JsonSerializerSettings
            {
                MaxDepth = 42,
                NullValueHandling = NullValueHandling.Ignore
            });

        NewtonsoftJsonMessageDeserializer<TestEventOne> deserializer2 = new(
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

        bool result = Equals(deserializer1, deserializer2);

        result.ShouldBeFalse();
    }
}
