﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProducerEndpointConfigurationBuilderSchemaRegistryExtensionsFixture
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public ProducerEndpointConfigurationBuilderSchemaRegistryExtensionsFixture()
    {
        _serviceProvider.GetService(typeof(IConfluentSchemaRegistryClientFactory)).Returns(Substitute.For<IConfluentSchemaRegistryClientFactory>());
    }

    [Fact]
    public void SerializeAsAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.SerializeAsAvro(serializer => serializer.ConnectToSchemaRegistry("test-url"));

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(
                serializer => serializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseModelIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(
                serializer => serializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldThrow_WhenTypeNotSpecified()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.SerializeAsJsonUsingSchemaRegistry(serializer => serializer.ConnectToSchemaRegistry("test-url"));

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsJsonUsingSchemaRegistry(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldSetSerializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsJsonUsingSchemaRegistry(
                serializer => serializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldSetSerializer_WhenUseModelIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsJsonUsingSchemaRegistry(
                serializer => serializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsProtobuf_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<ProtobufMessage> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsProtobuf(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
    }
}
