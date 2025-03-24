// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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
            EndpointResolver = endpointResolver
        };

        configuration.RawName.ShouldBe("raw-name");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSerializerIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { Serializer = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { EndpointResolver = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenStrategyIsNull()
    {
        TestProducerEndpointConfiguration configuration = GetValidConfiguration() with { Strategy = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
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

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("A unique friendly name for the endpoint is required when using the outbox produce strategy.");
    }

    private static TestProducerEndpointConfiguration GetValidConfiguration() => new("test");
}
