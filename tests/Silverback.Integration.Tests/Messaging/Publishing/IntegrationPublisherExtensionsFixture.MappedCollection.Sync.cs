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
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForMappedCollection()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedCollection()
    {
        List<int?> sources = [1, 2, null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        int count1 = 10;
        int count2 = 20;

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (envelope, source) => envelope
                .SetKafkaKey(envelope.Producer == producer1 ? $"{++count1}" : $"{++count2}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName));

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("11");
        capturedEnvelopes1[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("12");
        capturedEnvelopes1[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("13");
        capturedEnvelopes1[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1" });
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("21");
        capturedEnvelopes2[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2" });
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("22");
        capturedEnvelopes2[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("23");
        capturedEnvelopes2[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForMappedCollection_WhenPassingArgument()
    {
        List<int?> sources = [1, 2, null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            static (source, args) =>
                source == null ? null : new TestEventOne { Content = $"{source}-{args.CounterSource.Increment()}" },
            static (envelope, source, args) => envelope
                .SetKafkaKey(envelope.Producer == args.Producer1 ? $"{args.Counter1.Increment()}" : $"{args.Counter2.Increment()}")
                .AddHeader("x-source", source ?? -1)
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            (Counter1: new Counter(10), Counter2: new Counter(20), CounterSource: new Counter(), Producer1: producer1));

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("11");
        capturedEnvelopes1[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("12");
        capturedEnvelopes1[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("13");
        capturedEnvelopes1[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "1-1" });
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("21");
        capturedEnvelopes2[0].Headers["x-source"].ShouldBe("1");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(new TestEventOne { Content = "2-2" });
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("22");
        capturedEnvelopes2[1].Headers["x-source"].ShouldBe("2");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("23");
        capturedEnvelopes2[2].Headers["x-source"].ShouldBe("-1");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldInvokeSubscribersForMappedCollectionAccordingToEnableSubscribing()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" });

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldInvokeSubscribersForConfiguredMappedCollectionAccordingToEnableSubscribing()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            source => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _) =>
            {
            });

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldInvokeSubscribersForConfiguredMappedCollectionAccordingToEnableSubscribing_WhenPassingArgument()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two", true);
        (IProducer _, IProduceStrategyImplementation strategy3) = AddProducer<TestEventOne>("three", true);
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy1.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        await strategy3.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(envelopes => _ = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            sources,
            (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForMappedCollection()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            sources,
            static source => source == null ? null : new TestEventOne { Content = $"{source}" });

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForMappedCollectionAndPassingArgument()
    {
        List<int?> sources = [1, 2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            sources,
            static (source, _) => source == null ? null : new TestEventOne { Content = $"{source}" },
            (_, _, _) =>
            {
            },
            1);

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }
}
