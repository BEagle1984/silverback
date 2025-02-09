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
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());
        int count = 0;

        _publisher.WrapAndPublishBatch(
            messages,
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable_WhenEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());
        int count = 0;

        _publisher.WrapAndPublishBatch(
            messages,
            envelope => envelope
                .SetKafkaKey($"{++count}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName));

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            messages,
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgumentAndEnableSubscribing()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne?> messages = [message1, message2, null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", true);
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()),
            Arg.Any<CancellationToken>());

        _publisher.WrapAndPublishBatch(
            messages,
            static (envelope, counter) => envelope
                .SetKafkaKey($"{counter.Increment()}")
                .AddHeader("x-topic", envelope.EndpointConfiguration.RawName),
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(), CancellationToken.None);
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
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new(), new(), null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
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

        _publisher.WrapAndPublishBatch(messages);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrapAndPublishBatch_ShouldPublishToInternalBusForEnumerableAccordingToEnableSubscribing_WhenPassingArgument(bool enableSubscribing)
    {
        IEnumerable<TestEventOne?> messages = [new(), new(), null];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one", enableSubscribing);
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

        _publisher.WrapAndPublishBatch(
            messages,
            (_, _) =>
            {
            },
            1);

        if (enableSubscribing)
            await _publisher.Received(3).PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
        else
            await _publisher.DidNotReceive().PublishAsync(Arg.Any<IOutboundEnvelope<TestEventOne>>(), CancellationToken.None);
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(messages);

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerableAndPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            messages,
            (_, _) =>
            {
            },
            1);

        Exception exception = act.ShouldThrow<RoutingException>();
        exception.Message.ShouldBe("No producer found for message of type 'TestEventOne'.");
        strategy.ReceivedCalls().ShouldBeEmpty();
    }
}
