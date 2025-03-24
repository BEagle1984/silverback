// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using NSubstitute;
using Shouldly;
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

        TestProducerEndpointConfiguration configuration = builder
            .SetMessageId(message => message?.Content)
            .Build();

        configuration.MessageEnrichers.Count.ShouldBe(1);
        configuration.MessageEnrichers.Single().ShouldBeOfType<MessageIdOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder
            .SetMessageId<TestEventOne>(message => message?.Content)
            .Build();

        configuration.MessageEnrichers.Count.ShouldBe(1);
        configuration.MessageEnrichers.Single().ShouldBeOfType<MessageIdOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherWithEnvelopeBasedValueFunction()
    {
        TestProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder
            .SetMessageId(envelope => envelope.Headers.Count)
            .Build();

        configuration.MessageEnrichers.Count.ShouldBe(1);
        configuration.MessageEnrichers.Single().ShouldBeOfType<MessageIdOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void WithMessageId_ShouldAddMessageEnricherWithEnvelopeBasedValueFunctionForChildType()
    {
        TestProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder
            .SetMessageId<TestEventOne>(envelope => envelope.Headers.Count)
            .Build();

        configuration.MessageEnrichers.Count.ShouldBe(1);
        configuration.MessageEnrichers.Single().ShouldBeOfType<MessageIdOutboundHeadersEnricher<TestEventOne>>();
    }
}
