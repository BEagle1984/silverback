// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Messaging.Messages;

public class OutboundEnvelopeBuilderTests
{
    [Fact]
    public void Build_ShouldCreateTypedEnvelope()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.ShouldBeOfType<TestOutboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public void WithMessage_ShouldSetMessage()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();
        TestEventOne message = new();

        builder.WithMessage(message);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Message.ShouldBeSameAs(message);
    }

    [Fact]
    public void WithHeaders_ShouldSetHeaders()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];

        builder.WithHeaders(headers);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldBe(headers);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", "1");

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderObject()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader header = new("one", "1");

        builder.AddHeader(header);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(header);
    }

    [Fact]
    public void WithEndpointConfiguration_ShouldSetEndpointConfiguration()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();
        TestOutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpointConfiguration endpointConfiguration = new();

        builder.WithEndpointConfiguration(endpointConfiguration);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.EndpointConfiguration.ShouldBeSameAs(endpointConfiguration);
    }

    [Fact]
    public void WithProducer_ShouldSetProducer()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();
        TestOutboundEnvelopeBuilder<TestEventOne>.MockProducer producer = new();

        builder.WithProducer(producer);

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Producer.ShouldBeSameAs(producer);
    }

    [Fact]
    public void Build_ShouldSetMockEndpointConfiguration_WhenEndpointNotSpecified()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.EndpointConfiguration.ShouldBeOfType<TestOutboundEnvelopeBuilder<TestEventOne>.MockProducerEndpointConfiguration>();
    }

    [Fact]
    public void Build_ShouldSetMockProducer_WhenProducerNotSpecified()
    {
        TestOutboundEnvelopeBuilder<TestEventOne> builder = new();

        IOutboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Producer.ShouldBeOfType<TestOutboundEnvelopeBuilder<TestEventOne>.MockProducer>();
    }

    private sealed class TestOutboundEnvelopeBuilder<TMessage> : OutboundEnvelopeBuilder<TestOutboundEnvelopeBuilder<TMessage>, TestOutboundEnvelope<TMessage>, TMessage>
        where TMessage : class
    {
        protected override TestOutboundEnvelopeBuilder<TMessage> This => this;

        protected override TestOutboundEnvelope<TMessage> BuildCore(
            TMessage? message,
            MessageHeaderCollection? headers,
            ProducerEndpointConfiguration endpointConfiguration,
            IProducer producer) =>
            new(message, headers, endpointConfiguration, producer);
    }
}
