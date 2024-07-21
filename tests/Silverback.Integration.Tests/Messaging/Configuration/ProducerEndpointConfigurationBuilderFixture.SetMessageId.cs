// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void WithMessageId_ShouldAddMessageEnricher()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .SetMessageId(message => message?.Content)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<OutboundMessageIdHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .SetMessageId<TestEventOne>(message => message?.Content)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<OutboundMessageIdHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherWithEnvelopeBasedValueFunction()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .SetMessageId(envelope => envelope.Headers.Count)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<OutboundMessageIdHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherWithEnvelopeBasedValueFunctionForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration endpoint = builder
            .SetMessageId<TestEventOne>(envelope => envelope.Headers.Count)
            .Build();

        endpoint.MessageEnrichers.Should().HaveCount(1);
        endpoint.MessageEnrichers.Single().Should().BeOfType<OutboundMessageIdHeadersEnricher<TestEventOne>>();
    }
}
