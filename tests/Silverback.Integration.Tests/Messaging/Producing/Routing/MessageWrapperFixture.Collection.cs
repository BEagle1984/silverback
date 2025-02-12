// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Routing;

public partial class MessageWrapperFixture
{
    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two", true);
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

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer1, producer2], cancellationToken: cancellationToken);

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
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two", true);
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
        int count = 0;

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer1, producer2],
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            cancellationToken);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("4");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("5");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("6");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne?> messages = [message1, message2, null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two", true);
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

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer1, producer2],
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter(),
            cancellationToken);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Length.ShouldBe(3);
        capturedEnvelopes1[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes1[0].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[0].GetKafkaKey().ShouldBe("1");
        capturedEnvelopes1[0].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes1[1].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[1].GetKafkaKey().ShouldBe("2");
        capturedEnvelopes1[1].Headers["x-topic"].ShouldBe("one");
        capturedEnvelopes1[2].Message.ShouldBeNull();
        capturedEnvelopes1[2].EndpointConfiguration.RawName.ShouldBe("one");
        capturedEnvelopes1[2].GetKafkaKey().ShouldBe("3");
        capturedEnvelopes1[2].Headers["x-topic"].ShouldBe("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), cancellationToken);
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Length.ShouldBe(3);
        capturedEnvelopes2[0].Message.ShouldBeEquivalentTo(message1);
        capturedEnvelopes2[0].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[0].GetKafkaKey().ShouldBe("4");
        capturedEnvelopes2[0].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[1].Message.ShouldBeEquivalentTo(message2);
        capturedEnvelopes2[1].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[1].GetKafkaKey().ShouldBe("5");
        capturedEnvelopes2[1].Headers["x-topic"].ShouldBe("two");
        capturedEnvelopes2[2].Message.ShouldBeNull();
        capturedEnvelopes2[2].EndpointConfiguration.RawName.ShouldBe("two");
        capturedEnvelopes2[2].GetKafkaKey().ShouldBe("6");
        capturedEnvelopes2[2].Headers["x-topic"].ShouldBe("two");
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldInvokeSubscribersForCollectionAccordingToEnableSubscribing()
    {
        TestEventOne?[] messages = [new(), new(), null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two", true);
        (IProducer producer3, IProduceStrategyImplementation strategy3) = CreateProducer("three", true);
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

        await _messageWrapper.WrapAndProduceBatchAsync(messages, _publisher, [producer1, producer2, producer3], cancellationToken: cancellationToken);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }

    [Fact]
    public async Task WrapAndProduceBatchAsync_ShouldInvokeSubscribersForCollectionAccordingToEnableSubscribing_WhenPassingArgument()
    {
        TestEventOne?[] messages = [new(), new(), null];
        (IProducer producer1, IProduceStrategyImplementation strategy1) = CreateProducer("one");
        (IProducer producer2, IProduceStrategyImplementation strategy2) = CreateProducer("two", true);
        (IProducer producer3, IProduceStrategyImplementation strategy3) = CreateProducer("three", true);
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

        await _messageWrapper.WrapAndProduceBatchAsync(
            messages,
            _publisher,
            [producer1, producer2, producer3],
            (_, _) =>
            {
            },
            1,
            cancellationToken);

        // Expect to publish 3 messages twice (once per enabled producer)
        await _publisher.Received(6).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), cancellationToken);
    }
}
