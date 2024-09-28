// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        await _publisher.WrapAndPublishAsync(message);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceEnvelopesPassingCancellationToken()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishAsync(message, cancellationToken);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one"),
            cancellationToken);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two"),
            cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceTombstones()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishAsync<TestEventOne>(null, cancellationToken);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one"),
            cancellationToken);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two"),
            cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishAsync(
            message,
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            cancellationToken: cancellationToken);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            cancellationToken);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredTombstones()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        await _publisher.WrapAndPublishAsync<TestEventOne>(
            null,
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredEnvelopes_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishAsync(
            message,
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            "key",
            cancellationToken: cancellationToken);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            cancellationToken);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredTombstones_WhenPassingArgument()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        await _publisher.WrapAndPublishAsync(
            (TestEventOne?)null,
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            "key");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldThrow_WhenNoMatchingProducers()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishAsync(message);

        await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldThrow_WhenNoMatchingProducersAndPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishAsync(
            message,
            (_, _) =>
            {
            },
            1);

        await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().Should().BeEmpty();
    }
}
