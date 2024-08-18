// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
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

        envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public void WithMessage_ShouldSetMessage()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        TestEventOne message = new();

        builder.WithMessage(message);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Message.Should().BeSameAs(message);
    }

    [Fact]
    public void WithHeaders_ShouldSetHeaders()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader[] headers = [new MessageHeader("one", "1"), new MessageHeader("two", "2")];

        builder.WithHeaders(headers);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", "1");

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().Contain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderObject()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader header = new("one", "1");

        builder.AddHeader(header);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().Contain(header);
    }

    [Fact]
    public void WithEndpoint_ShouldSetEndpoint()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        OutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpoint endpoint = new();

        builder.WithEndpoint(endpoint);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Endpoint.Should().BeSameAs(endpoint);
    }

    [Fact]
    public void WithProducer_ShouldSetProducer()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();
        OutboundEnvelopeBuilder<TestEventOne>.MockProducer producer = new();

        builder.WithProducer(producer);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Producer.Should().BeSameAs(producer);
    }

    [Fact]
    public void Build_ShouldSetMockEndpoint_WhenEndpointNotSpecified()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Endpoint.Should().BeOfType<OutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpoint>();
        envelope.Endpoint.Configuration.Should().BeOfType<OutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpointConfiguration>();
    }

    [Fact]
    public void Build_ShouldSetMockProducer_WhenProducerNotSpecified()
    {
        OutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Producer.Should().BeOfType<OutboundEnvelopeBuilder<TestEventOne>.MockProducer>();
    }
}
