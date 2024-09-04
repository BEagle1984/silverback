// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(messages);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes1[2].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].GetKafkaKey().Should().Be("4");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].GetKafkaKey().Should().Be("5");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].GetKafkaKey().Should().Be("6");
        capturedEnvelopes2[2].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(3);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[2].Message.Should().Be(null);
        capturedEnvelopes1[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes1[2].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(3);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].GetKafkaKey().Should().Be("4");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].GetKafkaKey().Should().Be("5");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[2].Message.Should().Be(null);
        capturedEnvelopes2[2].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[2].GetKafkaKey().Should().Be("6");
        capturedEnvelopes2[2].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldPublishToInternalBusForCollectionAccordingToEnableSubscribing()
    {
        TestEventOne?[] messages = [new(), new(), null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy1.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy2.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy2.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy3.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy3.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(messages);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldPublishToInternalBusForCollectionAccordingToEnableSubscribing_WhenPassingArgument()
    {
        TestEventOne?[] messages = [new(), new(), null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy1.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy2.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy2.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));
        await strategy3.ProduceAsync(Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()));
        await strategy3.ProduceAsync(Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollection(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollectionAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }
}
