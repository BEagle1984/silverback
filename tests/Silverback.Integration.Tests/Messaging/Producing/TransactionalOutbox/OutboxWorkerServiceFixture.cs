// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
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
            TimeSpan.FromMilliseconds(50),
            outboxWorker,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        await outboxWorkerService.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        await outboxWorker.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldUseLock()
    {
        InMemoryLock memoryLock = new(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>());
        IOutboxWorker outboxWorker1 = Substitute.For<IOutboxWorker>();
        IOutboxWorker outboxWorker2 = Substitute.For<IOutboxWorker>();
        OutboxWorkerService outboxWorkerService1 = new(
            TimeSpan.FromMilliseconds(50),
            outboxWorker1,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());
        OutboxWorkerService outboxWorkerService2 = new(
            TimeSpan.FromMilliseconds(50),
            outboxWorker2,
            memoryLock,
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        CancellationTokenSource cancellationTokenSource1 = new();

        await outboxWorkerService1.StartAsync(cancellationTokenSource1.Token);
        await outboxWorkerService2.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        await outboxWorker1.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
        await outboxWorker2.DidNotReceive().ProcessOutboxAsync(Arg.Any<CancellationToken>());

        cancellationTokenSource1.Cancel();

        await Task.Delay(200);

        await outboxWorker2.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }
}
