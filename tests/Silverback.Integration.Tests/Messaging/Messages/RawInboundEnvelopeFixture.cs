// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class RawInboundEnvelopeFixture
{
    [Fact]
    public void Constructor_ShouldNotThrow_WhenRawMessageIsNull()
    {
        RawInboundEnvelope envelope = new(
            (Stream?)null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.ShouldNotBeNull();
    }

    [Fact]
    public void CloneReplacingRawMessage_ShouldClone()
    {
        RawInboundEnvelope envelope = new(
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        IRawInboundEnvelope newEnvelope = envelope.CloneReplacingRawMessage(new MemoryStream());

        newEnvelope.ShouldNotBeSameAs(envelope);
        newEnvelope.RawMessage.ShouldNotBeSameAs(envelope.RawMessage);
        newEnvelope.Headers.ShouldBe(envelope.Headers);
        newEnvelope.Consumer.ShouldBeSameAs(envelope.Consumer);
        newEnvelope.BrokerMessageIdentifier.ShouldBeSameAs(envelope.BrokerMessageIdentifier);
    }
}
