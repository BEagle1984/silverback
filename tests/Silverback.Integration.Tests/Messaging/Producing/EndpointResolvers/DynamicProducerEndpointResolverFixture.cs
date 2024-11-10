// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.EndpointResolvers;

public class DynamicProducerEndpointResolverFixture
{
    private readonly TestDynamicProducerEndpointResolver<TestEventOne> _endpointResolver = new("topic");

    [Fact]
    public void GetEndpoint_ShouldReturnEndpoint()
    {
        ProducerEndpoint endpoint = _endpointResolver.GetEndpoint(
            new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                null,
                new TestProducerEndpointConfiguration(),
                Substitute.For<IProducer>()));

        endpoint.Should().BeOfType<TestProducerEndpoint>();
        endpoint.RawName.Should().Be("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenMessageTypeMismatch()
    {
        Action act = () => _endpointResolver.GetEndpoint(
            new OutboundEnvelope<TestEventTwo>(
                new TestEventTwo(),
                null,
                new TestProducerEndpointConfiguration(),
                Substitute.For<IProducer>()));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The envelope must be of type Silverback.Messaging.Messages.IOutboundEnvelope`1[[Silverback.Tests.Types.Domain.TestEventOne*");
    }
}
