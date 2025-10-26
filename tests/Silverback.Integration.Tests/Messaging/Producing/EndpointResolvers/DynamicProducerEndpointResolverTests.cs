// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.EndpointResolvers;

public class DynamicProducerEndpointResolverTests
{
    [Fact]
    public void GetEndpoint_ShouldReturnEndpoint()
    {
        TestDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic");

        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic"));
        TestOutboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), producer);
        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(envelope);

        endpoint.ShouldBeOfType<TestProducerEndpoint>();
        endpoint.RawName.ShouldBe("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldThrow_WhenMessageTypeMismatch()
    {
        TestDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic");
        TestOutboundEnvelope<TestEventTwo> envelope = new(new TestEventTwo(), Substitute.For<IProducer>());

        Action act = () => endpointResolver.GetEndpoint(envelope);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldMatch(@"The envelope must be of type Silverback.Messaging.Messages.IOutboundEnvelope`1\[\[Silverback.Tests.Types.Domain.TestEventOne.*");
    }
}
