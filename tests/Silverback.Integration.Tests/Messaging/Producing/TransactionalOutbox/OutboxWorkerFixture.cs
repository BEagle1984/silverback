// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerFixture
{
    private readonly InMemoryOutbox _inMemoryOutbox = new();

    private readonly IOutboxWriter _outboxWriter;

    private readonly IProducerCollection _producerCollection = Substitute.For<IProducerCollection>();

    private readonly IProducer _producer1 = Substitute.For<IProducer>();

    private readonly IProducer _producer2 = Substitute.For<IProducer>();

    private readonly IProducerLogger<OutboxWorker> _producerLogger = Substitute.For<IProducerLogger<OutboxWorker>>();

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public OutboxWorkerFixture()
    {
        _outboxWriter = new InMemoryOutboxWriter(_inMemoryOutbox, Substitute.For<ISilverbackLogger<InMemoryOutboxWriter>>());

        _producerCollection.GetProducerForEndpoint("one").Returns(_producer1);
        _producerCollection.GetProducerForEndpoint("two").Returns(_producer2);

        _producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });
        _producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                FriendlyName = "two"
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessOutboxAsync_ShouldProduceMessages(bool enforceMessageOrder)
    {
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                    Task.Run(
                        async () =>
                        {
                            await Task.Delay(5);
                            callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                                .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                        }).FireAndForget());
        _producer2
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                    Task.Run(
                        async () =>
                        {
                            await Task.Delay(5);
                            callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                                .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                        }).FireAndForget());

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "two"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = enforceMessageOrder },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        AssertReceivedCalls(_producer1, 2);
        AssertReceivedCalls(_producer2, 1);
    }

    [Fact]
    public async Task ProcessQueue_ShouldRetryAndEnforceOrder_WhenProduceThrows()
    {
        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int tries = 0;
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                {
                    if (Interlocked.Increment(ref tries) is 2 or 5)
                        throw new InvalidOperationException("Test");

                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                        .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 2);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 5);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries
    }

    [Fact]
    public async Task ProcessQueue_ShouldRetry_WhenProduceThrowsAndNotEnforcingOrder()
    {
        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int tries = 0;
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                {
                    if (Interlocked.Increment(ref tries) is 2 or 5)
                        throw new InvalidOperationException("Test");

                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                        .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 10);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries
    }

    [Fact]
    public async Task ProcessQueue_ShouldRetryAndEnforceOrder_WhenProduceFails()
    {
        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int tries = 0;
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                {
                    if (Interlocked.Increment(ref tries) is 2 or 5)
                    {
                        callInfo.ArgAt<Action<Exception, OutboxWorker.ProduceState>>(3)
                            .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                    }
                    else
                    {
                        callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                            .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                    }
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 2);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 5);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries
    }

    [Fact]
    public async Task ProcessQueue_ShouldRetry_WhenProduceFailsAndNotEnforcingOrder()
    {
        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int tries = 0;
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                {
                    if (Interlocked.Increment(ref tries) is 2 or 5)
                    {
                        callInfo.ArgAt<Action<Exception, OutboxWorker.ProduceState>>(3)
                            .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                    }
                    else
                    {
                        callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                            .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                    }
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 10);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessQueue_ShouldHandleCancellation_WhenEnforcingOrder(bool enforceMessageOrder)
    {
        CancellationTokenSource cancellationTokenSource = new();

        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int processed = 0;
        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
                    Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
                    Arg.Any<OutboxWorker.ProduceState>()))
            .Do(
                callInfo =>
                {
                    Task.Run(
                        async () =>
                        {
                            await Task.Delay(10);
                            callInfo.ArgAt<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(2)
                                .Invoke(null, callInfo.ArgAt<OutboxWorker.ProduceState>(4));
                        }).FireAndForget();

                    if (Interlocked.Increment(ref processed) == 5)
                        cancellationTokenSource.Cancel();
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = enforceMessageOrder },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(cancellationTokenSource.Token);
        AssertReceivedCalls(_producer1, 5);

        cancellationTokenSource.IsCancellationRequested.Should().BeTrue();

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 10);
    }

    private static void AssertReceivedCalls(IProducer producer, int requiredNumberOfCalls) =>
        producer.Received(requiredNumberOfCalls).RawProduce(
            Arg.Any<byte[]>(),
            Arg.Any<IReadOnlyCollection<MessageHeader>>(),
            Arg.Any<Action<IBrokerMessageIdentifier?, OutboxWorker.ProduceState>>(),
            Arg.Any<Action<Exception, OutboxWorker.ProduceState>>(),
            Arg.Any<OutboxWorker.ProduceState>());
}
