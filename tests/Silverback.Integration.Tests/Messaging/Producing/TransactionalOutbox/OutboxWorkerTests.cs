// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerTests
{
    private readonly InMemoryOutbox _inMemoryOutbox = new();

    private readonly IOutboxWriter _outboxWriter;

    private readonly IProducerCollection _producerCollection = Substitute.For<IProducerCollection>();

    private readonly IProducer _producer1 = Substitute.For<IProducer>();

    private readonly IProducer _producer2 = Substitute.For<IProducer>();

    private readonly ISilverbackLogger<OutboxWorker> _logger = Substitute.For<ISilverbackLogger<OutboxWorker>>();

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public OutboxWorkerTests()
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
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
                Task.Run(async () =>
                {
                    await Task.Delay(5);
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget());
        _producer2
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
                Task.Run(async () =>
                {
                    await Task.Delay(5);
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget());

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "two"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = enforceMessageOrder },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        bool result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 2);
        AssertReceivedCalls(_producer2, 1);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldContinueTraceOfStoredMessage()
    {
        IReadOnlyCollection<MessageHeader>? producedHeaders = null;
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                producedHeaders = callInfo.ArgAt<IReadOnlyCollection<MessageHeader>>(1);
                callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2).Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
            });

        await _outboxWriter.AddAsync(
            new OutboxMessage(
                [0x01],
                [
                    new MessageHeader(DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"),
                    new MessageHeader("x-custom", "value")
                ],
                "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()),
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        producedHeaders.ShouldNotBeNull();
        producedHeaders.ShouldContain(new MessageHeader("x-custom", "value"));
        MessageHeader traceIdHeader = producedHeaders.Where(header => header.Name == DefaultMessageHeaders.TraceId).ShouldHaveSingleItem();
        ActivityContext context = ActivityContext.Parse(traceIdHeader.Value!, null);
        context.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        context.SpanId.ToString().ShouldNotBe("b7ad6b7169203331");
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldStartIndependentActivityForEachStoredMessage()
    {
        ConcurrentQueue<Activity> produceActivities = new();
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                produceActivities.Enqueue(Activity.Current.ShouldNotBeNull());

                // Acknowledged while the next message is produced, so that the activity is still running
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget();
            });

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()),
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        produceActivities.Count.ShouldBe(2);
        produceActivities.ShouldAllBe(activity => activity.Parent == null && activity.IsStopped);
        produceActivities.Select(activity => activity.TraceId).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldStopActivity_WhenProduceThrows()
    {
        Activity? produceActivity = null;
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(_ =>
            {
                produceActivity = Activity.Current;
                throw new InvalidOperationException("test");
            });

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        produceActivity.ShouldNotBeNull().IsStopped.ShouldBeTrue();
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
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                if (Interlocked.Increment(ref tries) is 2 or 5)
                    throw new InvalidOperationException("Test");

                callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                    .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
            });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        Func<Task> act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await act.ShouldThrowAsync<OutboxProcessingException>();
        AssertReceivedCalls(_producer1, 2);

        act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await act.ShouldThrowAsync<OutboxProcessingException>();
        AssertReceivedCalls(_producer1, 5);

        bool result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeFalse();
        AssertReceivedCalls(_producer1, 12);
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
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                if (Interlocked.Increment(ref tries) is 2 or 5)
                    throw new InvalidOperationException("Test");

                callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                    .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
            });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        bool result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 10);

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeFalse();
        AssertReceivedCalls(_producer1, 12);
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
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                if (Interlocked.Increment(ref tries) is 2 or 5)
                {
                    callInfo.ArgAt<Action<Exception, (OutboxMessage, Activity)>>(3)
                        .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }
                else
                {
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }
            });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        Func<Task> act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await act.ShouldThrowAsync<OutboxProcessingException>();
        AssertReceivedCalls(_producer1, 2);

        act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await act.ShouldThrowAsync<OutboxProcessingException>();
        AssertReceivedCalls(_producer1, 5);

        bool result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeFalse();
        AssertReceivedCalls(_producer1, 12);
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
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                if (Interlocked.Increment(ref tries) is 2 or 5)
                {
                    callInfo.ArgAt<Action<Exception, (OutboxMessage, Activity)>>(3)
                        .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }
                else
                {
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }
            });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        bool result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 10);

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeTrue();
        AssertReceivedCalls(_producer1, 12); // 10 messages + 2 retries

        result = await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        result.ShouldBeFalse();
        AssertReceivedCalls(_producer1, 12);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessQueue_ShouldHandleCancellation(bool enforceMessageOrder)
    {
        CancellationTokenSource cancellationTokenSource = new();

        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        }

        int processed = 0;
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(10);
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget();

                if (Interlocked.Increment(ref processed) == 5)
                    cancellationTokenSource.Cancel();
            });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = enforceMessageOrder },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        Func<Task> act = () => outboxWorker.ProcessOutboxAsync(cancellationTokenSource.Token);
        await act.ShouldThrowAsync<OperationCanceledException>();
        AssertReceivedCalls(_producer1, 5);

        cancellationTokenSource.IsCancellationRequested.ShouldBeTrue();

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        AssertReceivedCalls(_producer1, 10);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldWaitForPendingProduces_WhenGetProducerThrows()
    {
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
                Task.Run(async () =>
                {
                    await Task.Delay(300);
                    callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                        .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget());
        _producerCollection.GetProducerForEndpoint("unknown").Returns(_ => throw new InvalidOperationException("Unknown endpoint"));

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "unknown"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        // The slow message must have been acknowledged in the same pass, only the failed one is left
        outboxWorker.LastPassFailed.ShouldBeTrue();
        (await outboxWorker.GetLengthAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldSetLastPassFailed_WhenProduceFailsAsynchronously()
    {
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo =>
                Task.Run(async () =>
                {
                    await Task.Delay(20);
                    callInfo.ArgAt<Action<Exception, (OutboxMessage, Activity)>>(3)
                        .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<(OutboxMessage, Activity)>(4));
                }).FireAndForget());

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        outboxWorker.LastPassFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldNotProduceNextMessage_WhenPreviousFailedAsynchronouslyAndEnforcingOrder()
    {
        Action? failPendingProduce = null;
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo => failPendingProduce = () => callInfo.ArgAt<Action<Exception, (OutboxMessage, Activity)>>(3)
                .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<(OutboxMessage, Activity)>(4)));

        // The first produce fails while the producer of the next message is being resolved, after the failure check of the loop
        _producerCollection.GetProducerForEndpoint("two").Returns(_ =>
        {
            failPendingProduce.ShouldNotBeNull().Invoke();

            return _producer2;
        });

        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "two"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        Func<Task> act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        await act.ShouldThrowAsync<OutboxProcessingException>();

        AssertReceivedCalls(_producer1, 1);
        AssertReceivedCalls(_producer2, 0);
        outboxWorker.LastPassFailed.ShouldBeTrue();
        outboxWorker.LastPassProducedCount.ShouldBe(0);
        (await outboxWorker.GetLengthAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldThrowWithoutAcknowledging_WhenDisposedWhileProducing()
    {
        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, "one"));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, "two"));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()),
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _logger);

        // The first message is produced, while the second one is still pending when the worker is disposed
        _producer1
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(callInfo => callInfo.ArgAt<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(2)
                .Invoke(null, callInfo.ArgAt<(OutboxMessage, Activity)>(4)));
        _producer2
            .When(producer => producer.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
                Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
                Arg.Any<(OutboxMessage, Activity)>()))
            .Do(_ => outboxWorker.Dispose());

        Func<Task> act = () => outboxWorker.ProcessOutboxAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        // The worker can't wait for the pending produce, so it doesn't acknowledge the produced message either
        await act.ShouldThrowAsync<ObjectDisposedException>();
        AssertReceivedCalls(_producer1, 1);
        AssertReceivedCalls(_producer2, 1);
        (await outboxWorker.GetLengthAsync()).ShouldBe(2);
    }

    private static void AssertReceivedCalls(IProducer producer, int requiredNumberOfCalls) =>
        producer.Received(requiredNumberOfCalls).RawProduce(
            Arg.Any<byte[]>(),
            Arg.Any<IReadOnlyCollection<MessageHeader>>(),
            Arg.Any<Action<IBrokerMessageIdentifier?, (OutboxMessage, Activity)>>(),
            Arg.Any<Action<Exception, (OutboxMessage, Activity)>>(),
            Arg.Any<(OutboxMessage, Activity)>());
}
