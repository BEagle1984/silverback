// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ProducerEndpointConfigurationFixture
{
    [Fact]
    public void RawName_ShouldReturnEndpointRawName()
    {
        IProducerEndpointResolver<TestProducerEndpoint> endpointResolver = Substitute.For<IProducerEndpointResolver<TestProducerEndpoint>>();
        endpointResolver.RawName.Returns("raw-name");

        TestProducerEndpointConfiguration configuration = new()
        {
            Endpoint = endpointResolver
        };

        configuration.RawName.Should().Be("raw-name");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSerializerIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { Serializer = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { Endpoint = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenStrategyIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { Strategy = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenUsingOutboxWithoutFriendlyName(string? friendlyName)
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Strategy = new OutboxProduceStrategy(new InMemoryOutboxSettings()),
            FriendlyName = friendlyName
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("A friendly unique name for the endpoint is required when using the outbox produce strategy.");
    }

    private static TestProducerEndpointConfiguration GetValidConfiguration() => new("test");
}
