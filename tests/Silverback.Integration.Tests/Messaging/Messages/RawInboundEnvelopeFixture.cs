// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using NSubstitute;
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

        envelope.Should().NotBeNull();
    }

    [Fact]
    public void GetMessageId_ShouldReturnHeaderValue()
    {
        RawInboundEnvelope envelope = new(
            (Stream?)null,
            [new MessageHeader("x-message-id", "test-id")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.GetMessageId().Should().Be("test-id");
    }

    [Fact]
    public void GetMessageId_ShouldReturnNull_WhenHeaderNotSet()
    {
        RawInboundEnvelope envelope = new(
            (Stream?)null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.GetMessageId().Should().BeNull();
    }
}
