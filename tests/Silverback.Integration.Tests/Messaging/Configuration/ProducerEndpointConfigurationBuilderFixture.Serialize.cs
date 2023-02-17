// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void ImplicitSerializeAsJson_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void ImplicitSerializeBinary_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<BinaryMessage> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void SerializeAsJson_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.SerializeAsJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
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

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
        endpoint.Serializer.As<JsonMessageSerializer>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void ProduceBinaryMessages_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpoint = builder.ProduceBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer>();
    }
}
