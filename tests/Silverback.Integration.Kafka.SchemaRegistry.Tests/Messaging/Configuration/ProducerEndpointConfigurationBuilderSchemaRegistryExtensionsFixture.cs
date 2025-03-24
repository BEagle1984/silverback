// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.ShouldBeOfType<AvroMessageSerializer<TestEventOne>>();
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

        endpointConfiguration.Serializer.ShouldBeOfType<AvroMessageSerializer<TestEventOne>>();
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

        endpointConfiguration.Serializer.ShouldBeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldThrow_WhenTypeNotSpecified()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.SerializeAsJsonUsingSchemaRegistry(serializer => serializer.ConnectToSchemaRegistry("test-url"));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void SerializeAsJsonUsingSchemaRegistry_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsJsonUsingSchemaRegistry(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
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

        endpointConfiguration.Serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
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

        endpointConfiguration.Serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsProtobuf_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<ProtobufMessage> builder = new(_serviceProvider);

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsProtobuf(serializer => serializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Serializer.ShouldBeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
    }
}
