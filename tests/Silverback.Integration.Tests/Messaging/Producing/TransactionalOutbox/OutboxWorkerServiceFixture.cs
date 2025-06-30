// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerServiceFixture
{
    [Fact]
    public async Task StartAsync_ShouldProcessOutboxEveryInterval()
    {
        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromMilliseconds(50)
            },
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Count() >= 2);

        await outboxWorker.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldProcessAllMessagesInSingleIteration()
    {
        int calls = 0;

        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromHours(2)
            },
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => ++calls < 3
                    ? Task.FromResult(true)
                    : Task.FromResult(false));

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Count() == 3);

        await outboxWorker.Received(3).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldRetry_WhenProcessingFails()
    {
        int calls = 0;

        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromHours(2),
                InitialRetryDelay = TimeSpan.FromMilliseconds(10)
            },
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => ++calls < 3
                    ? Task.FromResult(true)
                    : Task.FromResult(false));

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Count() == 3);

        await outboxWorker.Received(3).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldNotRetry_WhenProcessingSucceeds()
    {
        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromMilliseconds(1),
                InitialRetryDelay = TimeSpan.FromHours(2)
            },
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Count() > 1);

        await outboxWorker.Received(Quantity.Within(2, int.MaxValue)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldNotRetry_WhenCanceled()
    {
        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromMilliseconds(1),
                InitialRetryDelay = TimeSpan.FromHours(2)
            },
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>())
            .Throws(_ => new TaskCanceledException());

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Count() > 1);

        await outboxWorker.Received(Quantity.Within(2, int.MaxValue)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldUseLock()
    {
        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker1 = Substitute.For<IOutboxWorker>();
        IOutboxWorker outboxWorker2 = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService1 = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromMilliseconds(50)
            },
            outboxWorker1,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());
        OutboxWorkerService outboxWorkerService2 = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = TimeSpan.FromMilliseconds(50)
            },
            outboxWorker2,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        CancellationTokenSource cancellationTokenSource1 = new();
        CancellationTokenSource cancellationTokenSource2 = new();

        await outboxWorkerService1.StartAsync(cancellationTokenSource1.Token);
        await outboxWorkerService2.StartAsync(cancellationTokenSource2.Token);

        await AsyncTestingUtil.WaitAsync(
            () => outboxWorker1.ReceivedCalls().Count() >= 2 ||
                  outboxWorker2.ReceivedCalls().Count() >= 2);

        if (outboxWorker1.ReceivedCalls().Any())
        {
            await outboxWorker1.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
            await outboxWorker2.DidNotReceive().ProcessOutboxAsync(Arg.Any<CancellationToken>());
            await cancellationTokenSource1.CancelAsync();
        }
        else
        {
            await outboxWorker1.DidNotReceive().ProcessOutboxAsync(Arg.Any<CancellationToken>());
            await outboxWorker2.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
            await cancellationTokenSource2.CancelAsync();
        }

        await AsyncTestingUtil.WaitAsync(
            () => outboxWorker1.ReceivedCalls().Count() >= 2 &&
                  outboxWorker2.ReceivedCalls().Count() >= 2);

        await outboxWorker1.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
        await outboxWorker2.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }
}
