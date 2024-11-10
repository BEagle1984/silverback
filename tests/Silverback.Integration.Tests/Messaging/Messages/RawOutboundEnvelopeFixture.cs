// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class RawOutboundEnvelopeFixture
{
    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.AddHeader("one", "1").AddHeader("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeader()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.AddOrReplaceHeader("one", "1").AddOrReplaceHeader("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldReplaceHeader()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.AddOrReplaceHeader("one", "1").AddOrReplaceHeader("one", "2");

        envelope.Headers.Should().NotContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "2"));
    }

    [Fact]
    public void AddHeaderIfNotExists_ShouldAddHeader()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.AddHeaderIfNotExists("one", "1").AddHeaderIfNotExists("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddHeaderIfNotExists_ShouldNotReplaceHeader()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.AddHeaderIfNotExists("one", "1").AddHeaderIfNotExists("one", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().NotContainEquivalentOf(new MessageHeader("one", "2"));
    }

    [Fact]
    public void SetMessageId_ShouldSetMessageId()
    {
        RawOutboundEnvelope envelope = new(null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        envelope.SetMessageId("one").SetMessageId("two");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.MessageId, "two"));
    }

    [Fact]
    public void GetMessageId_ShouldReturnHeaderValue()
    {
        RawOutboundEnvelope envelope = new(
            [new MessageHeader("x-message-id", "test-id")],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.GetMessageId().Should().Be("test-id");
    }

    [Fact]
    public void GetMessageId_ShouldReturnNull_WhenHeaderNotSet()
    {
        RawOutboundEnvelope envelope = new(
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.GetMessageId().Should().BeNull();
    }
}
