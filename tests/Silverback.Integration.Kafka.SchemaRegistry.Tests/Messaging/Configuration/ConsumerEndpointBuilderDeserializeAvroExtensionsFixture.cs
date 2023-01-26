// Copyright (c) 2023 Sergio Aquilini
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
    public void DeserializeAvro_ShouldSetSerializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder.DeserializeAvro().Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetSerializer_WhenUseTypeWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(serializer => serializer.UseType<TestEventOne>())
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeAvro_ShouldSetSerializer_WhenUseTypeIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpointConfiguration = builder
            .DeserializeAvro(serializer => serializer.UseType(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
    }
}
