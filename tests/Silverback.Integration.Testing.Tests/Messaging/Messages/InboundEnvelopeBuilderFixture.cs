// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Messaging.Messages;

public class InboundEnvelopeBuilderFixture
{
    [Fact]
    public void WithRawMessage_ShouldSetRawMessage()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        Stream rawMessage = new MemoryStream();

        builder.WithRawMessage(rawMessage);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.RawMessage.Should().BeSameAs(rawMessage);
    }

    [Fact]
    public void Build_ShouldCreateTypedEnvelope()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Should().BeOfType<InboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public void WithMessage_ShouldSetMessage()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        TestEventOne message = new();

        builder.WithMessage(message);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Message.Should().BeSameAs(message);
    }

    [Fact]
    public void WithHeaders_ShouldSetHeaders()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader[] headers = [new("one", "1"), new("two", "2")];

        builder.WithHeaders(headers);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", "1");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().Contain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderWithObjectValue()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.AddHeader("one", 1);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().Contain(new MessageHeader("one", "1"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeader()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("two", "2");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().HaveCount(2);
        envelope.Headers.Should().Contain(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeaderWithObjectValue()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("two", 2);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().HaveCount(2);
        envelope.Headers.Should().Contain(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldReplaceHeader()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        builder.AddHeader("one", "1");

        builder.AddOrReplaceHeader("one", "2");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().HaveCount(1);
        envelope.Headers.Should().Contain(new MessageHeader("one", "2"));
    }

    [Fact]
    public void AddHeader_ShouldAddHeaderObject()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        MessageHeader header = new("one", "1");

        builder.AddHeader(header);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Headers.Should().Contain(header);
    }

    [Fact]
    public void WithEndpoint_ShouldSetEndpoint()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        InboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpoint endpoint = new();

        builder.WithEndpoint(endpoint);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Endpoint.Should().BeSameAs(endpoint);
    }

    [Fact]
    public void WithConsumer_ShouldSetConsumer()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        InboundEnvelopeBuilder<TestEventOne>.MockConsumer consumer = new();

        builder.WithConsumer(consumer);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Consumer.Should().BeSameAs(consumer);
    }

    [Fact]
    public void WithIdentifier_ShouldSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        InboundEnvelopeBuilder<TestEventOne>.MockBrokerMessageIdentifier identifier = new();

        builder.WithIdentifier(identifier);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.Should().BeSameAs(identifier);
    }

    [Fact]
    public void Build_ShouldSetMockEndpoint_WhenEndpointNotSpecified()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Endpoint.Should().BeOfType<InboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpoint>();
        envelope.Endpoint.Configuration.Should().BeOfType<InboundEnvelopeBuilder<TestEventOne>.MockConsumerEndpointConfiguration>();
    }

    [Fact]
    public void Build_ShouldSetMockConsumer_WhenConsumerNotSpecified()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.Consumer.Should().BeOfType<InboundEnvelopeBuilder<TestEventOne>.MockConsumer>();
    }

    [Fact]
    public void Build_ShouldSetMockIdentifier_WhenIdentifierNotSpecified()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        IInboundEnvelope<TestEventOne> envelope = builder.Build();

        envelope.BrokerMessageIdentifier.Should().BeOfType<InboundEnvelopeBuilder<TestEventOne>.MockBrokerMessageIdentifier>();
    }
}
