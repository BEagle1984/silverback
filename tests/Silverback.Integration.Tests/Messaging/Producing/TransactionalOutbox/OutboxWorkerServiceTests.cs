// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerServiceTests
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
            .Returns(_ => ++calls < 3
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
            .Returns(_ => ++calls < 3
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

        await AsyncTestingUtil.WaitAsync(() => outboxWorker1.ReceivedCalls().Count() >= 2 ||
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

        await AsyncTestingUtil.WaitAsync(() => outboxWorker1.ReceivedCalls().Count() >= 2 &&
                                               outboxWorker2.ReceivedCalls().Count() >= 2);

        await outboxWorker1.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
        await outboxWorker2.Received(Quantity.Within(2, 1000)).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessUntilEmptyAsync_ShouldProcessUntilEmpty()
    {
        int calls = 0;
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(++calls < 3));
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromHours(2));

        await outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None);

        calls.ShouldBe(3);
    }

    [Fact]
    public async Task SuspendAutomaticProcessingAsync_ShouldNotWaitForFullDrain()
    {
        int[] calls = [0];
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(
            async _ =>
            {
                Interlocked.Increment(ref calls[0]);
                await Task.Delay(10);
                return true;
            });
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(10));

        await outboxWorkerService.StartAsync(CancellationToken.None);
        await AsyncTestingUtil.WaitAsync(() => Volatile.Read(ref calls[0]) >= 2);

        await outboxWorkerService.SuspendAutomaticProcessingAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        outboxWorkerService.IsAutomaticProcessingSuspended.ShouldBeTrue();

        int callsAfterSuspend = Volatile.Read(ref calls[0]);
        await Task.Delay(150);
        Volatile.Read(ref calls[0]).ShouldBe(callsAfterSuspend);

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteLockedAsync_ShouldNotHoldSemaphoreDuringRetryDelay()
    {
        int calls = 0;
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(
            _ => Interlocked.Increment(ref calls) == 1
                ? Task.FromException<bool>(new InvalidOperationException("test"))
                : Task.FromResult(false));
        OutboxWorkerSettings settings = new(new InMemoryOutboxSettings())
        {
            Interval = TimeSpan.FromMilliseconds(10),
            InitialRetryDelay = TimeSpan.FromHours(1),
            MaxRetryDelay = TimeSpan.FromHours(1)
        };
        using OutboxWorkerService outboxWorkerService = new(
            settings,
            outboxWorker,
            new InMemoryLock(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>()),
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

        await outboxWorkerService.StartAsync(CancellationToken.None);
        await AsyncTestingUtil.WaitAsync(() => Volatile.Read(ref calls) >= 1);

        // The scheduled pass failed and is now waiting for the (very long) retry delay
        await Task.Delay(100);

        await outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessUntilEmptyAsync_ShouldPropagateExceptions()
    {
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Throws(new OutboxProcessingException("test"));
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromHours(2));

        await Should.ThrowAsync<OutboxProcessingException>(outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None));

        // The semaphore must have been released
        await Should.ThrowAsync<OutboxProcessingException>(outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SuspendAutomaticProcessingAsync_ShouldStopScheduledProcessing()
    {
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(10));

        await outboxWorkerService.SuspendAutomaticProcessingAsync(CancellationToken.None);
        await outboxWorkerService.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        outboxWorker.ReceivedCalls().ShouldBeEmpty();

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ResumeAutomaticProcessing_ShouldRestartScheduledProcessing()
    {
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(10));

        await outboxWorkerService.SuspendAutomaticProcessingAsync(CancellationToken.None);
        await outboxWorkerService.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        outboxWorkerService.ResumeAutomaticProcessing();

        await AsyncTestingUtil.WaitAsync(() => outboxWorker.ReceivedCalls().Any());
        outboxWorker.ReceivedCalls().ShouldNotBeEmpty();

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SuspendAutomaticProcessingAsync_ShouldWaitForRunningScheduledPass()
    {
        TaskCompletionSource<bool> passResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(_ => Interlocked.Increment(ref calls) == 1 ? passResult.Task : Task.FromResult(false));
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(10));

        await outboxWorkerService.StartAsync(CancellationToken.None);
        await AsyncTestingUtil.WaitAsync(() => Volatile.Read(ref calls) == 1);

        Task suspendTask = outboxWorkerService.SuspendAutomaticProcessingAsync(CancellationToken.None);

        await Task.Delay(100);
        suspendTask.IsCompleted.ShouldBeFalse();

        passResult.SetResult(false);
        await suspendTask.WaitAsync(TimeSpan.FromSeconds(5));

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SuspendAutomaticProcessingAsync_ShouldSuspendRightAway_WhenWaitIsCanceled()
    {
        TaskCompletionSource<bool> passResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(_ => passResult.Task);
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromHours(1));

        // A running pass holds the semaphore
        Task manualPass = outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(outboxWorkerService.SuspendAutomaticProcessingAsync(cancellationTokenSource.Token));

        outboxWorkerService.IsAutomaticProcessingSuspended.ShouldBeTrue();

        passResult.SetResult(false);
        await manualPass;
    }

    [Fact]
    public async Task ProcessUntilEmptyAsync_ShouldNotRunConcurrentlyWithScheduledProcessing()
    {
        int concurrentCalls = 0;
        int maxConcurrentCalls = 0;
        int calls = 0;
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(
            async _ =>
            {
                int current = Interlocked.Increment(ref concurrentCalls);
                int observedMax;
                while ((observedMax = Volatile.Read(ref maxConcurrentCalls)) < current &&
                       Interlocked.CompareExchange(ref maxConcurrentCalls, current, observedMax) != observedMax)
                {
                }

                Interlocked.Increment(ref calls);

                await Task.Delay(20);

                Interlocked.Decrement(ref concurrentCalls);
                return false;
            });
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(1));

        await outboxWorkerService.StartAsync(CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            await outboxWorkerService.ProcessUntilEmptyAsync(CancellationToken.None);
        }

        await outboxWorkerService.StopAsync(CancellationToken.None);

        calls.ShouldBeGreaterThanOrEqualTo(5);
        maxConcurrentCalls.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteLockedAsync_ShouldWaitMinimumDelay_WhenRoundIsSkipped(bool suspended)
    {
        TaskCompletionSource<bool> passResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(_ => passResult.Task);
        using TestableOutboxWorkerService service = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { Interval = TimeSpan.Zero },
            outboxWorker);

        // The round is skipped because the processing is suspended or a manual pass holds the semaphore
        Task manualPass = Task.CompletedTask;

        if (suspended)
            service.SuspendAutomaticProcessing();
        else
            manualPass = service.ProcessUntilEmptyAsync(CancellationToken.None);

        Stopwatch stopwatch = Stopwatch.StartNew();
        await service.ExecuteRoundAsync(CancellationToken.None);
        stopwatch.Stop();

        // A zero interval must not make the skipped rounds spin
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(80));

        passResult.SetResult(false);
        await manualPass;
    }

    [Fact]
    public async Task ProcessUntilEmptyAsync_ShouldThrow_WhenProduceFailsAsynchronously()
    {
        InMemoryOutbox outbox = new();
        IProducerCollection producers = CreateFailingProducers(true, () => { });
        await new InMemoryOutboxWriter(outbox, Substitute.For<ISilverbackLogger<InMemoryOutboxWriter>>())
            .AddAsync(new OutboxMessage([0x01], null, "one"));
        OutboxWorkerSettings settings = new(new InMemoryOutboxSettings()) { EnforceMessageOrder = false };
        using OutboxWorkerService service = new(
            settings,
            new OutboxWorker(settings, new InMemoryOutboxReader(outbox), producers, Substitute.For<ISilverbackLogger<OutboxWorker>>()),
            new InMemoryLock(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>()),
            new SilverbackLoggerSubstitute<OutboxWorkerService>());
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));

        await Should.ThrowAsync<OutboxProcessingException>(service.ProcessUntilEmptyAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ProcessUntilEmptyAsync_ShouldMakeScheduledProcessingYieldAfterCurrentBatch()
    {
        using CancellationTokenSource manualCancellationTokenSource = new();
        CancellationToken manualToken = manualCancellationTokenSource.Token;
        int[] manualCalls = [0];
        IOutboxWorker outboxWorker = Substitute.For<IOutboxWorker>();
        outboxWorker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(
            async callInfo =>
            {
                CancellationToken cancellationToken = callInfo.Arg<CancellationToken>();

                if (cancellationToken == manualToken)
                    Interlocked.Increment(ref manualCalls[0]);

                await Task.Delay(5);
                cancellationToken.ThrowIfCancellationRequested();

                // Like a worker that never ends because a message keeps failing
                return true;
            });
        using OutboxWorkerService outboxWorkerService = CreateService(outboxWorker, TimeSpan.FromMilliseconds(10));

        await outboxWorkerService.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        Task manualPass = outboxWorkerService.ProcessUntilEmptyAsync(manualCancellationTokenSource.Token);

        // The manual pass gets the semaphore although the scheduled loop never finishes by itself
        await AsyncTestingUtil.WaitAsync(() => Volatile.Read(ref manualCalls[0]) > 0, TimeSpan.FromSeconds(5));
        Volatile.Read(ref manualCalls[0]).ShouldBeGreaterThan(0);

        await manualCancellationTokenSource.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(manualPass);

        await outboxWorkerService.StopAsync(CancellationToken.None);
    }

    private static IProducerCollection CreateFailingProducers(bool failAsynchronously, Action onProduce)
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic") { FriendlyName = "one" });
        producer
            .When(p => p.RawProduce(
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                Arg.Any<Action<IBrokerMessageIdentifier?, OutboxMessage>>(),
                Arg.Any<Action<Exception, OutboxMessage>>(),
                Arg.Any<OutboxMessage>()))
            .Do(callInfo =>
            {
                onProduce.Invoke();

                if (!failAsynchronously)
                    throw new InvalidOperationException("Test");

                Task.Run(async () =>
                {
                    await Task.Delay(20);
                    callInfo.ArgAt<Action<Exception, OutboxMessage>>(3)
                        .Invoke(new InvalidOperationException("Test"), callInfo.ArgAt<OutboxMessage>(4));
                }).FireAndForget();
            });
        IProducerCollection producers = Substitute.For<IProducerCollection>();
        producers.GetProducerForEndpoint("one").Returns(producer);
        return producers;
    }

    private static OutboxWorkerService CreateService(IOutboxWorker outboxWorker, TimeSpan interval) =>
        new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings())
            {
                Interval = interval
            },
            outboxWorker,
            new InMemoryLock(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>()),
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

    private sealed class TestableOutboxWorkerService : OutboxWorkerService
    {
        public TestableOutboxWorkerService(OutboxWorkerSettings settings, IOutboxWorker outboxWorker)
            : base(
                settings,
                outboxWorker,
                new InMemoryLock(new InMemoryLockSettings("test"), new SilverbackLoggerSubstitute<InMemoryLock>()),
                new SilverbackLoggerSubstitute<OutboxWorkerService>())
        {
        }

        public Task ExecuteRoundAsync(CancellationToken stoppingToken) => ExecuteLockedAsync(stoppingToken);
    }
}
