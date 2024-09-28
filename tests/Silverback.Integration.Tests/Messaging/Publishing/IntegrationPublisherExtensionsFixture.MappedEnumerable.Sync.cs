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
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForMappedEnumerable()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].Message.Should().BeNull();
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForMappedEnumerable_WhenEnableSubscribing()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].Message.Should().BeNull();
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedEnumerable()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        int count = 0;

        _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedEnumerable_WhenEnableSubscribing()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        int count = 0;

        _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedEnumerable_WhenPassingArgument()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static (source, counter) =>
            {
                counter.Increment();
                return source == null ? null : new TestEventOne { Content = $"{source}-{counter.Value}" };
            },
            static (envelope, source, counter) => envelope
                .SetKafkaKey($"{counter.Value}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static (source, counter) =>
            {
                counter.Increment();
                return source == null ? null : new TestEventOne { Content = $"{source}-{counter.Value}" };
            },
            static (envelope, source, counter) => envelope
                .SetKafkaKey($"{counter.Value}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.Endpoint.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(3);
        capturedEnvelopes[0].Message.Should().BeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].GetKafkaKey().Should().Be("1");
        capturedEnvelopes[0].Headers["x-source"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().BeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].GetKafkaKey().Should().Be("2");
        capturedEnvelopes[1].Headers["x-source"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[2].Message.Should().Be(null);
        capturedEnvelopes[2].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[2].GetKafkaKey().Should().Be("3");
        capturedEnvelopes[2].Headers["x-source"].Should().Be("-1");
        capturedEnvelopes[2].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForMappedEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForMappedEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForMappedEnumerable()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForMappedEnumerableAndPassingArgument()
    {
        IEnumerable<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            sources,
            static (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().Should().BeEmpty();
    }
}
