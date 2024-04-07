// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class OutboundEnvelopeFixture
{
    [Fact]
    public void Constructor_ShouldSetRawMessageFromByteArray()
    {
        byte[] message = [1, 2, 3];
        OutboundEnvelope outboundEnvelope = new(message, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(message);
        outboundEnvelope.RawMessage.As<MemoryStream>().ToArray().Should().BeEquivalentTo(message);
    }

    [Fact]
    public void Constructor_ShouldSetRawMessageFromStream()
    {
        MemoryStream stream = new([1, 2, 3]);
        OutboundEnvelope outboundEnvelope = new(stream, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(stream);
        outboundEnvelope.RawMessage.Should().BeSameAs(stream);
    }

    [Fact]
    public void CloneReplacingRawMessage_ShouldClone()
    {
        OutboundEnvelope<TestEventOne> outboundEnvelope = new(
            new TestEventOne { Content = "old" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        IOutboundEnvelope newEnvelope = outboundEnvelope.CloneReplacingRawMessage(new MemoryStream());

        newEnvelope.Should().NotBeSameAs(outboundEnvelope);
        newEnvelope.Should().BeEquivalentTo(outboundEnvelope, options => options.Excluding(envelope => envelope.RawMessage));
        newEnvelope.RawMessage.Should().NotBeSameAs(outboundEnvelope.RawMessage);
    }
}
