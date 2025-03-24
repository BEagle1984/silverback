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

public class ConsumerEndpointConfigurationBuilderSchemaRegistryExtensionsFixture
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public ConsumerEndpointConfigurationBuilderSchemaRegistryExtensionsFixture()
    {
        _serviceProvider.GetService(typeof(IConfluentSchemaRegistryClientFactory)).Returns(Substitute.For<IConfluentSchemaRegistryClientFactory>());
    }

    [Fact]
    public void DeserializeAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.DeserializeAvro(deserializer => deserializer.ConnectToSchemaRegistry("test-url"));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(deserializer => deserializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(
                deserializer => deserializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(
                deserializer => deserializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingSchemaRegistry_ShouldThrow_WhenTypeNotSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.DeserializeJsonUsingSchemaRegistry(deserializer => deserializer.ConnectToSchemaRegistry("test-url"));

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void DeserializeJsonUsingSchemaRegistry_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeJsonUsingSchemaRegistry(deserializer => deserializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingSchemaRegistry_ShouldSetDeserializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeJsonUsingSchemaRegistry(
                deserializer => deserializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingSchemaRegistry_ShouldSetDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeJsonUsingSchemaRegistry(
                deserializer => deserializer
                    .ConnectToSchemaRegistry("test-url")
                    .UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeProtobuf_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<ProtobufMessage> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeProtobuf(deserializer => deserializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Deserializer.ShouldBeOfType<ProtobufMessageDeserializer<ProtobufMessage>>();
    }
}
