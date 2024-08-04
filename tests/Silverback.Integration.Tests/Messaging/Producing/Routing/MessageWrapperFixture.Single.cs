// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public partial class MessageWrapperFixture
{
    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceEnvelopes()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(message, _publisher, [producer1, producer2]);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceTombstones()
    {
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync<TestEventOne>(null, _publisher, [producer1, producer2]);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(
            message,
            _publisher,
            [producer1, producer2],
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredTombstones()
    {
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync<TestEventOne>(
            null,
            _publisher,
            [producer1, producer2],
            static envelope => envelope
                .SetKafkaKey("key")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredEnvelopes_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(
            message,
            _publisher,
            [producer1, producer2],
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            "key");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldProduceConfiguredTombstones_WhenPassingArgument()
    {
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two");

        await _messageWrapper.WrapAndProduceAsync(
            (TestEventOne?)null,
            _publisher,
            [producer1, producer2],
            static (envelope, key) => envelope
                .SetKafkaKey(key)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            "key");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "one" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == null && envelope.Endpoint.RawName == "two" &&
                    envelope.GetKafkaKey() == "key" &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldPublishToInternalBusAccordingToEnableSubscribing()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", true);
        (IProducer producer3, IProduceStrategyImplementation _) = CreateProducer("three", true);

        await _messageWrapper.WrapAndProduceAsync(message, _publisher, [producer1, producer2, producer3]);

        await _publisher.Received(2).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Fact]
    public async Task WrapAndProduceAsync_ShouldPublishToInternalBusAccordingToEnableSubscribing_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer producer1, IProduceStrategyImplementation _) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation _) = CreateProducer("two", true);
        (IProducer producer3, IProduceStrategyImplementation _) = CreateProducer("three", true);

        await _messageWrapper.WrapAndProduceAsync(
            message,
            _publisher,
            [producer1, producer2, producer3],
            (_, _) =>
            {
            },
            1);

        await _publisher.Received(2).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }
}
