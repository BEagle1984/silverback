﻿// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public partial class MessageWrapperFixture
{
    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer]);

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
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer]);

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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
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
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new TestEventOne(), new TestEventOne(), null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()));
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer]);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new TestEventOne(), new TestEventOne(), null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()));
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()));

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            (_, _) =>
            {
            },
            1);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>());
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable()
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer, producer2]);

        await act.Should().ThrowAsync<RoutingException>()
            .WithMessage(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an Array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable_WhenPassingArgument()
    {
        IEnumerable<TestEventOne> messages = [new TestEventOne(), new TestEventOne()];
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
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an Array or any type implementing IReadOnlyCollection.");
    }
}