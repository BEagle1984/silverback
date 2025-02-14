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
                    capturedEnvelopes1 = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishBatchAsync(messages, cancellationToken);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
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
        CancellationToken cancellationToken = new(false);
        int count1 = 10;
        int count2 = 20;

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            envelope => envelope
                .SetKafkaKey(envelope.Producer == producer1 ? $"{++count1}" : $"{++count2}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("11");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("12");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("13");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("21");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("22");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("23");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
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
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            static (envelope, args) => envelope
                .SetKafkaKey(envelope.Producer == args.Producer1 ? $"{args.Counter1.Increment()}" : $"{args.Counter2.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            (Counter1: new Counter(10), Counter2: new Counter(20), Producer1: producer1),
            cancellationToken);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("11");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("12");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("13");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("21");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("22");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("23");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldInvokeSubscribersForCollectionAccordingToEnableSubscribing()
    {
        TestEventOne?[] messages = [new(), new(), null];
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
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishBatchAsync(messages, cancellationToken);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldInvokeSubscribersForCollectionAccordingToEnableSubscribing_WhenPassingArgument()
    {
        TestEventOne?[] messages = [new(), new(), null];
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
        CancellationToken cancellationToken = new(false);

        await _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1,
            cancellationToken);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(messages);

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollectionAndPassingArgument()
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
            1);

        Exception exception = await act.ShouldThrowAsync<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }
}
