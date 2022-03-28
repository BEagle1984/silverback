// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProducerEndpointBuilderSerializeAsAvroExtensionsTests
{
    [Fact]
    public void SerializeAsAvro_WithoutType_ExceptionThrown()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        Action act = () => builder.SerializeAsAvro();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SerializeAsAvro_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder.SerializeAsAvro().Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_Configure_SchemaRegistryAndSerializerConfigSet()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder.SerializeAsAvro(
            serializer => serializer
                .Configure(
                    schemaRegistryConfig =>
                    {
                        schemaRegistryConfig.Url = "some-url";
                    },
                    serializerConfig =>
                    {
                        serializerConfig.BufferBytes = 42;
                    })).Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
        endpointConfiguration.Serializer.As<AvroMessageSerializer<TestEventOne>>().SchemaRegistryConfig.Url.Should().Be("some-url");
        endpointConfiguration.Serializer.As<AvroMessageSerializer<TestEventOne>>().AvroSerializerConfig.BufferBytes.Should().Be(42);
    }
}
