// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldSetJsonMessageSerializerByDefault()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.Should().BeOfType<JsonMessageSerializer>();
        configuration.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageSerializerByDefault_WhenMessageTypeIsBinaryMessage()
    {
        TestProducerEndpointConfigurationBuilder<BinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.Should().BeOfType<BinaryMessageSerializer>();
        configuration.Serializer.Should().NotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void SerializeAsJson_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJson().Build();

        configuration.Serializer.Should().BeOfType<JsonMessageSerializer>();
        configuration.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void SerializeAsJson_ShouldSetSerializerAndOptions()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJson(
            serializer => serializer.Configure(
                options =>
                {
                    options.MaxDepth = 42;
                })).Build();

        configuration.Serializer.Should().BeOfType<JsonMessageSerializer>();
        configuration.Serializer.As<JsonMessageSerializer>().Options!.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void ProduceBinaryMessages_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceBinaryMessages().Build();

        configuration.Serializer.Should().BeOfType<BinaryMessageSerializer>();
    }
}
