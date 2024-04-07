// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ConsumerEndpointBuilderDeserializeAvroExtensionsFixture
{
    [Fact]
    public void DeserializeAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        Action act = () => builder.DeserializeAvro();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder.DeserializeAvro().Build();

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(deserializer => deserializer.UseModel<TestEventOne>())
            .Build();

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(deserializer => deserializer.UseModel(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Deserializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }
}
