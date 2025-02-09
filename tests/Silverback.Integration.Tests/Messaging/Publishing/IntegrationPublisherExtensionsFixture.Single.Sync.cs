// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
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
    public async Task WrapAndPublish_ShouldProduceEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(message);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceTombstones()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish<TestEventOne>(null);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(
            message,
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName));

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredTombstones()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish<TestEventOne>(
            null,
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName));

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredEnvelopes_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(
            message,
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            "key");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.EndpointConfiguration.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredTombstones_WhenPassingArgument()
    {
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(
            (TestEventOne?)null,
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            "key");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"),
            CancellationToken.None);
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.EndpointConfiguration.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"),
            CancellationToken.None);
    }

    [Fact]
    public void WrapAndPublish_ShouldThrow_WhenNoMatchingProducers()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublish(message);

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void WrapAndPublish_ShouldThrowOrIgnore_WhenNoMatchingProducersAndPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublish(
            message,
            (_, _) =>
            {
            },
            1);

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }
}
