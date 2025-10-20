// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Messaging.Messages;

public class InboundEnvelopeBuilderTests
{
    [Fact]
    public void WithRawMessage_ShouldSetRawMessage()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        Stream rawMessage = new MemoryStream();

        builder.WithRawMessage(rawMessage);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.RawMessage.ShouldBeSameAs(rawMessage);
    }

    [Fact]
    public void Build_ShouldCreateTypedEnvelope()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.ShouldBeOfType<TestInboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public void WithMessage_ShouldSetMessage()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        TestEventOne message = new();

        builder.WithMessage(message);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Message.ShouldBeSameAs(message);
    }

    [Fact]
    public void WithHeaders_ShouldSetHeaders()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];

        builder.WithHeaders(headers);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldBe(headers);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", "1");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderWithObjectValue()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", 1);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeader()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("two", "2");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Count.ShouldBe(2);
        envelope.Headers.ShouldContain(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeaderWithObjectValue()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("two", 2);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Count.ShouldBe(2);
        envelope.Headers.ShouldContain(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldReplaceHeader()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("one", "2");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldContain(new MessageHeader("one", "2"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderObject()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader header = new("one", "1");

        builder.AddHeader(header);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.ShouldContain(header);
    }

    [Fact]
    public void WithEndpoint_ShouldSetEndpoint()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        TestInboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpoint endpoint = new();

        builder.WithEndpoint(endpoint);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Endpoint.ShouldBeSameAs(endpoint);
    }

    [Fact]
    public void WithConsumer_ShouldSetConsumer()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        TestInboundEnvelopeBuilder<TestEventOne>.MockConsumer consumer = new();

        builder.WithConsumer(consumer);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Consumer.ShouldBeSameAs(consumer);
    }

    [Fact]
    public void WithIdentifier_ShouldSetIdentifier()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();
        TestBrokerMessageIdentifier identifier = new();

        builder.WithIdentifier(identifier);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.ShouldBeSameAs(identifier);
    }

    [Fact]
    public void Build_ShouldSetMockEndpoint_WhenEndpointNotSpecified()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Endpoint.ShouldBeOfType<TestInboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpoint>();
        envelope.Endpoint.Configuration.ShouldBeOfType<TestInboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpointConfiguration>();
    }

    [Fact]
    public void Build_ShouldSetMockConsumer_WhenConsumerNotSpecified()
    {
        TestInboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Consumer.ShouldBeOfType<TestInboundEnvelopeBuilder<TestEventOne>.MockConsumer>();
    }

    private sealed class TestInboundEnvelopeBuilder<TMessage> : InboundEnvelopeBuilder<TestInboundEnvelopeBuilder<TMessage>, TestInboundEnvelope<TMessage>, TMessage>
        where TMessage : class
    {
        protected override TestInboundEnvelopeBuilder<TMessage> This => this;

        protected override TestInboundEnvelope<TMessage> BuildCore(
            TMessage? message,
            Stream? rawMessage,
            MessageHeaderCollection? headers,
            ConsumerEndpoint endpoint,
            IConsumer consumer,
            IBrokerMessageIdentifier? identifier)
        {
            TestInboundEnvelope<TMessage> envelope = new(message, rawMessage, endpoint, consumer, identifier ?? new TestBrokerMessageIdentifier());

            if (headers != null)
                envelope.Headers.AddRange(headers);

            return envelope;
        }
    }

    private sealed class TestBrokerMessageIdentifier : IBrokerMessageIdentifier
    {
        public bool Equals(IBrokerMessageIdentifier? other) => this == other;

        public string ToLogString() => "mock";

        public string ToVerboseLogString() => "mock";
    }
}
