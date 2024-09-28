// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public partial class MessageWrapperFixture
{
    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer], cancellationToken: cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForAsyncEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer], cancellationToken: cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne?> messages = new[] { message1, message2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
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
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForAsyncEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IAsyncEnumerable<TestEventOne?> messages = new[] { new TestEventOne(), new TestEventOne(), null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer], cancellationToken: cancellationToken);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForAsyncEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IAsyncEnumerable<TestEventOne?> messages = new[] { new TestEventOne(), new TestEventOne(), null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            (_, _) =>
            {
            },
            1,
            cancellationToken);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForAsyncEnumerable()
    {
        IAsyncEnumerable<TestEventOne> messages = new[] { new TestEventOne(), new TestEventOne() }.ToAsyncEnumerable();
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer, producer2]);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an Array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForAsyncEnumerable_WhenPassingArgument()
    {
        IAsyncEnumerable<TestEventOne> messages = new[] { new TestEventOne(), new TestEventOne() }.ToAsyncEnumerable();
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer, producer2],
            (_, _) =>
            {
            },
            1);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an Array or any type implementing IReadOnlyCollection.");
    }
}
