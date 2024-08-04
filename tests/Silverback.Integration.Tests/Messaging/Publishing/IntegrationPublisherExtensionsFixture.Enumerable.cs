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
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        await _publisher.WrapAndPublishBatchAsync(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].Message.Should().BeNull();
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].Message.Should().BeNull();
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));
        int count = 0;

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndPublishBatchAsync_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new TestEventOne(), new TestEventOne(), null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()));
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(messages);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndPublishBatchAsync_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new TestEventOne(), new TestEventOne(), null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()));
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()));

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerable(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
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
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerableAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
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
