// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void ImplicitSerializeAsJson_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void ImplicitSerializeAsJson_WithSetMessageType_TypedSerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void ImplicitSerializeBinary_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<BinaryMessage> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void ImplicitSerializeBinary_WithCustomMessageType_TypedSerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void SerializeAsJson_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void SerializeAsJson_WithSetMessageType_TypedSerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_UseFixedType_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder
            .SerializeAsJson(serializer => serializer.UseFixedType(typeof(TestEventOne)))
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_WithOptions_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson(
            serializer => serializer.WithOptions(
                new JsonSerializerOptions
                {
                    MaxDepth = 42
                })).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.As<JsonMessageSerializer<object>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SerializeAsJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson(
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
    public void ProduceBinaryMessages_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.ProduceBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
    }

    [Fact]
    public void ProduceBinaryMessages_UseFixedType_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder
            .ProduceBinaryMessages(serializer => serializer.UseModel<CustomBinaryMessage>())
            .Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    private sealed class CustomBinaryMessage : BinaryMessage
    {
    }
}
