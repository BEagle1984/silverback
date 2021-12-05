// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerConfigurationBuilderTests
{
    [Fact]
    public void ImplicitDeserializeJson_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void ImplicitDeserializeJson_WithSetMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void ImplicitDeserializeBinary_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<BinaryMessage> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void ImplicitDeserializeBinary_WithCustomMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void DeserializeJson_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void DeserializeJson_WithSetMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_UseFixedType_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder
            .DeserializeJson(serializer => serializer.UseFixedType(typeof(TestEventOne))).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_WithOptions_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(
            serializer => serializer.WithOptions(
                new JsonSerializerOptions
                {
                    MaxDepth = 42
                })).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.As<JsonMessageSerializer<object>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(
            serializer => serializer
                .UseFixedType<TestEventOne>()
                .WithOptions(
                    new JsonSerializerOptions
                    {
                        MaxDepth = 42
                    })).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        endpoint.Serializer.As<JsonMessageSerializer<TestEventOne>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void ConsumeBinaryMessages_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void ConsumeBinaryMessages_WithSetMessageType_SerializerSet()
    {
        TestConsumerConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void ConsumeBinaryMessages_WithInvalidMessageType_ExceptionThrown()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        Action act = () => builder.ConsumeBinaryMessages();

        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("The type *.TestEventOne does not implement IBinaryMessage. *");
    }

    [Fact]
    public void ConsumeBinaryMessages_MessageTypeWithoutEmptyConstructor_ExceptionThrown()
    {
        Action act = () =>
        {
            TestConsumerConfigurationBuilder<BinaryMessageWithoutDefaultConstructor> builder = new();
            builder.ConsumeBinaryMessages();
        };

        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("The type *+BinaryMessageWithoutDefaultConstructor does not have a default constructor. *");
    }

    [Fact]
    public void ConsumeBinaryMessages_UseModel_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder
            .ConsumeBinaryMessages(serializer => serializer.UseModel<CustomBinaryMessage>())
            .Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    private sealed class CustomBinaryMessage : BinaryMessage
    {
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class BinaryMessageWithoutDefaultConstructor : BinaryMessage
    {
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required for testing.")]
        public BinaryMessageWithoutDefaultConstructor(string value)
        {
        }
    }
}
