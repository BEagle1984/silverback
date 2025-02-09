// Copyright (c) 2024 Sergio Aquilini
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
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForMappedAsyncEnumerable()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            cancellationToken: cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForMappedAsyncEnumerable_WhenEnableSubscribing()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            cancellationToken: cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForMappedAsyncEnumerable()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
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
            sources,
            _publisher,
            [producer],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForMappedAsyncEnumerable_WhenEnableSubscribing()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
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
            sources,
            _publisher,
            [producer],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForMappedAsyncEnumerable_WhenPassingArgument()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static (source, counter) =>
            {
                counter.Increment();
                return source == null ? null : new TestEventOne { Content = $"{source}-{counter.Value}" };
            },
            static (envelope, source, counter) => envelope
                .SetKafkaKey($"{counter.Value}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForMappedAsyncEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static (source, counter) =>
            {
                counter.Increment();
                return source == null ? null : new TestEventOne { Content = $"{source}-{counter.Value}" };
            },
            static (envelope, source, counter) => envelope
                .SetKafkaKey($"{counter.Value}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter(),
            cancellationToken);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Length.ShouldBe(3);
        capturedEnvelopes[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes[2].Message.ShouldBeNull();
        capturedEnvelopes[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes[2].Headers["x-topic"].ShouldBe("one");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForMappedAsyncEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            cancellationToken: cancellationToken);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndProduceBatchAsync_ShouldPublishToInternalBusForMappedAsyncEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        (IProducer producer, IProduceStrategyImplementation strategy) = CreateProducer("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer],
            static (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
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
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForMappedAsyncEnumerable()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer, producer2],
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldThrow_WhenMultipleProducersSpecifiedForMappedAsyncEnumerable_WhenPassingArgument()
    {
        IAsyncEnumerable<int?> sources = new int?[] { 1, 2, null }.ToAsyncEnumerable();
        IProducer producer = Substitute.For<IProducer>();
        IProducer producer2 = Substitute.For<IProducer>();

        Func<Task> act = () => _messageWrapper.WrapAndProduceBatchAsync(
            sources,
            _publisher,
            [producer, producer2],
            static (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
    }
}
