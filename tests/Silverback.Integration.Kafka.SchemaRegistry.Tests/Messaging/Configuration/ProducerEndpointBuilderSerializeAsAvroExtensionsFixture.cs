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

public class ProducerEndpointBuilderSerializeAsAvroExtensionsFixture
{
    [Fact]
    public void SerializeAsAvro_ShouldThrow_WhenTypeNotSpecified()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        Action act = () => builder.SerializeAsAvro();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder.SerializeAsAvro().Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseTypeWithGenericArgumentIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.UseType<TestEventOne>())
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsAvro_ShouldSetSerializer_WhenUseTypeIsCalled()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new();

        TestProducerEndpointConfiguration endpointConfiguration = builder
            .SerializeAsAvro(serializer => serializer.UseType(typeof(TestEventOne)))
            .Build();

        endpointConfiguration.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }
}
