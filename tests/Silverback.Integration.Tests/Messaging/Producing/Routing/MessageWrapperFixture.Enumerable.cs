// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
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
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer], cancellationToken: cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
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
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer], null, cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
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
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
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
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
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
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
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
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer],
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndProduceBatchAsync_ShouldInvokeSubscribersForEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new(), new(), null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
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
    public async Task WrapAndProduceBatchAsync_ShouldInvokeSubscribersForEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new(), new(), null];
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
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
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable()
    {
        IEnumerable<TestEventOne> messages = [new(), new()];
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer, producer2]);

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForEnumerable_WhenPassingArgument()
    {
        IEnumerable<TestEventOne> messages = [new(), new()];
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

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
    }
}
