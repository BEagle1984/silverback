// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Messaging.Messages;

public class OutboundEnvelopeBuilderFixture
{
    [Fact]
    public void Build_ShouldCreateTypedEnvelope()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.ShouldBeOfType<OutboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public void WithMessage_ShouldSetMessage()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        TestEventOne message = new();

        builder.WithMessage(message);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Message.ShouldBeSameAs(message);
    }

    [Fact]
    public void WithHeaders_ShouldSetHeaders()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];

        builder.WithHeaders(headers);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldBe(headers);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", "1");

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderObject()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader header = new("one", "1");

        builder.AddHeader(header);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(header);
    }

    [Fact]
    public void WithEndpointConfiguration_ShouldSetEndpointConfiguration()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        OutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpointConfiguration endpointConfiguration = new();

        builder.WithEndpointConfiguration(endpointConfiguration);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.EndpointConfiguration.ShouldBeSameAs(endpointConfiguration);
    }

    [Fact]
    public void WithProducer_ShouldSetProducer()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        OutboundEnvelopeBuilder<TestEventOne>.MockProducer producer = new();

        builder.WithProducer(producer);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Producer.ShouldBeSameAs(producer);
    }

    [Fact]
    public void Build_ShouldSetMockEndpointConfiguration_WhenEndpointNotSpecified()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.EndpointConfiguration.ShouldBeOfType<OutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpointConfiguration>();
    }

    [Fact]
    public void Build_ShouldSetMockProducer_WhenProducerNotSpecified()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Producer.ShouldBeOfType<OutboundEnvelopeBuilder<TestEventOne>.MockProducer>();
    }
}
