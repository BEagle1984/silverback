// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ConsumerEndpointBuilderDeserializeAvroExtensionsFixture
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public ConsumerEndpointBuilderDeserializeAvroExtensionsFixture()
    {
        _serviceProvider.GetService(typeof(IConfluentSchemaRegistryClientFactory)).Returns(Substitute.For<IConfluentSchemaRegistryClientFactory>());
    }

    [Fact]
    public void DeserializeAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(_serviceProvider);

        Action act = () => builder.DeserializeAvro(deserializer => deserializer.ConnectToSchemaRegistry("test-url"));

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(_serviceProvider);

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(deserializer => deserializer.ConnectToSchemaRegistry("test-url"))
            .Build();

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
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

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
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

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }
}
