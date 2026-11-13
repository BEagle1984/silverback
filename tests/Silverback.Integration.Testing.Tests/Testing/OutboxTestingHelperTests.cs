// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Lock;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Testing;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Testing;

public class OutboxTestingHelperTests
{
    private static readonly TestOutboxSettings Outbox1 = new("outbox1");

    private static readonly TestOutboxSettings Outbox2 = new("outbox2");

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnPendingMessagesWithoutRemovingThem()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 3).AddOutbox(Outbox2, 2);
        fixture.AddWorker(Outbox1);
        fixture.AddProducer(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        IReadOnlyList<OutboxMessage> messages = await helper.GetMessagesAsync();

        messages.Count.ShouldBe(5);
        (await helper.GetLengthAsync()).ShouldBe(5);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldFilterByOutbox()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 3).AddOutbox(Outbox2, 2);
        fixture.AddWorker(Outbox1);
        fixture.AddWorker(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        (await helper.GetMessagesAsync(Outbox2)).Count.ShouldBe(2);
        (await helper.GetLengthAsync(Outbox1)).ShouldBe(3);
        (await helper.IsEmptyAsync(Outbox1)).ShouldBeFalse();
    }

    [Fact]
    public async Task IsEmptyAsync_ShouldDiscoverOutboxesFromProducersWhenNoWorkerIsRegistered()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 1);
        fixture.AddProducer(Outbox1);
        fixture.AddProducer(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        (await helper.IsEmptyAsync()).ShouldBeFalse();
        (await helper.GetMessagesAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task IsEmptyAsync_ShouldReturnTrue_WhenNoOutboxIsFound()
    {
        OutboxTestingHelper helper = new Fixture().Build();

        (await helper.IsEmptyAsync()).ShouldBeTrue();
        (await helper.GetMessagesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllMessages()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 2500).AddOutbox(Outbox2, 3);
        fixture.AddWorker(Outbox1);
        fixture.AddWorker(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        await helper.ClearAsync(Outbox1);

        (await helper.GetLengthAsync(Outbox1)).ShouldBe(0);
        (await helper.GetLengthAsync(Outbox2)).ShouldBe(3);

        await helper.ClearAsync();

        (await helper.IsEmptyAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldCompleteWhenOutboxIsEmptied()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 2);
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        Task waitTask = helper.WaitUntilEmptyAsync(TimeSpan.FromSeconds(10)).AsTask();
        await Task.Delay(100);
        waitTask.IsCompleted.ShouldBeFalse();

        await helper.ClearAsync();

        await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldThrow_WhenTimeoutElapses()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 2);
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        await Should.ThrowAsync<TimeoutException>(helper.WaitUntilEmptyAsync(TimeSpan.FromMilliseconds(200)).AsTask());
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldThrowRightAway_WhenNoReaderCanBeCreatedForTheSpecifiedOutbox()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 1);
        OutboxTestingHelper helper = fixture.Build();

        // The fixture's reader factory throws for the unknown settings
        await Should.ThrowAsync<KeyNotFoundException>(helper.WaitUntilEmptyAsync(TimeSpan.FromMinutes(1), Outbox2).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldThrowOperationCanceledException_WhenTokenIsCanceled()
    {
        OutboxTestingHelper helper = new Fixture().Build();
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(helper.WaitUntilEmptyAsync(cancellationTokenSource.Token).AsTask());
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldIgnoreOutboxesNotBeingDrained()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 2).AddOutbox(Outbox2, 2);
        fixture.AddWorker(Outbox1);
        fixture.AddProducer(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        // Outbox2 has no worker and outbox1 is paused
        await helper.PauseWorkerAsync();

        await helper.WaitUntilEmptyAsync(TimeSpan.FromSeconds(5));
        (await helper.IsEmptyIgnoringErrorsAsync(null)).ShouldBeTrue();

        // The explicit settings are always honored
        await Should.ThrowAsync<TimeoutException>(helper.WaitUntilEmptyAsync(TimeSpan.FromMilliseconds(200), Outbox2).AsTask());
        await Should.ThrowAsync<TimeoutException>(helper.WaitUntilEmptyAsync(TimeSpan.FromMilliseconds(200), Outbox1).AsTask());

        // The full discovery is unaffected
        (await helper.IsEmptyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldIgnoreErrors()
    {
        using Fixture fixture = new();
        IOutboxReader reader = Substitute.For<IOutboxReader>();
        int calls = 0;
        reader.GetLengthAsync().Returns(_ => ++calls == 1 ? throw new InvalidOperationException("test") : Task.FromResult(0));
        fixture.Readers[Outbox1] = reader;
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        await helper.WaitUntilEmptyAsync(TimeSpan.FromSeconds(5));

        calls.ShouldBe(2);
    }

    [Fact]
    public async Task ProcessAsync_ShouldProcessAllWorkers()
    {
        using Fixture fixture = new();
        IOutboxWorker worker1 = fixture.AddWorker(Outbox1);
        IOutboxWorker worker2 = fixture.AddWorker(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        await helper.ProcessAsync();

        await worker1.Received(1).ProcessOutboxAsync(Arg.Any<CancellationToken>());
        await worker2.Received(1).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldProcessOnlyMatchingWorker()
    {
        using Fixture fixture = new();
        IOutboxWorker worker1 = fixture.AddWorker(Outbox1);
        IOutboxWorker worker2 = fixture.AddWorker(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        await helper.ProcessAsync(Outbox2);

        await worker1.DidNotReceive().ProcessOutboxAsync(Arg.Any<CancellationToken>());
        await worker2.Received(1).ProcessOutboxAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ShouldPropagateExceptions()
    {
        using Fixture fixture = new();
        IOutboxWorker worker = fixture.AddWorker(Outbox1);
        worker.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ => throw new OutboxProcessingException("test"));
        OutboxTestingHelper helper = fixture.Build();

        await Should.ThrowAsync<OutboxProcessingException>(helper.ProcessAsync().AsTask());
    }

    [Fact]
    public async Task ProcessAsync_ShouldThrow_WhenNoWorkerIsRegistered()
    {
        using Fixture fixture = new();
        fixture.AddProducer(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        await Should.ThrowAsync<InvalidOperationException>(helper.ProcessAsync().AsTask());
        await Should.ThrowAsync<InvalidOperationException>(helper.ProcessAsync(Outbox1).AsTask());
    }

    [Fact]
    public async Task ProcessAsync_ShouldThrow_WhenNoWorkerIsRegisteredForTheOutbox()
    {
        using Fixture fixture = new();
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        await Should.ThrowAsync<InvalidOperationException>(helper.ProcessAsync(Outbox2).AsTask());
    }

    [Fact]
    public async Task PauseWorkerAsync_ShouldSuspendAllWorkersRightAway()
    {
        using Fixture fixture = new();
        TaskCompletionSource<bool> passResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IOutboxWorker worker1 = fixture.AddWorker(Outbox1);
        worker1.ProcessOutboxAsync(Arg.Any<CancellationToken>()).Returns(_ => passResult.Task);
        fixture.AddWorker(Outbox2);
        OutboxTestingHelper helper = fixture.Build();

        // A running pass on the first worker
        Task manualPass = helper.ProcessAsync(Outbox1).AsTask();

        Task pauseTask = helper.PauseWorkerAsync().AsTask();

        pauseTask.IsCompleted.ShouldBeFalse();
        fixture.Services.ShouldAllBe(service => service.IsAutomaticProcessingSuspended);

        passResult.SetResult(false);
        await pauseTask.WaitAsync(TimeSpan.FromSeconds(5));
        await manualPass;
    }

    [Fact]
    public async Task PauseWorkerAsync_ShouldSuspendAndResumeWorker()
    {
        using Fixture fixture = new();
        IOutboxWorker worker = fixture.AddWorker(Outbox1, TimeSpan.FromMilliseconds(10));
        OutboxTestingHelper helper = fixture.Build();
        OutboxWorkerService service = fixture.Services.Single();

        await helper.PauseWorkerAsync();
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        worker.ReceivedCalls().ShouldBeEmpty();

        helper.ResumeWorker();

        await AsyncTestingUtil.WaitAsync(() => worker.ReceivedCalls().Any());
        worker.ReceivedCalls().ShouldNotBeEmpty();

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PauseWorkerAsync_ShouldThrow_WhenNoWorkerIsRegisteredForTheOutbox()
    {
        using Fixture fixture = new();
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        await Should.ThrowAsync<InvalidOperationException>(helper.PauseWorkerAsync(Outbox2).AsTask());
        Should.Throw<InvalidOperationException>(() => helper.ResumeWorker(Outbox2));
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldIgnoreOutboxesOfPausedServices()
    {
        using Fixture fixture = new Fixture().AddOutbox(Outbox1, 2);
        fixture.AddWorker(Outbox1);
        OutboxTestingHelper helper = fixture.Build();

        fixture.Services.Single().Pause();

        await helper.WaitUntilEmptyAsync(TimeSpan.FromSeconds(5));

        fixture.Services.Single().Resume();

        await Should.ThrowAsync<TimeoutException>(helper.WaitUntilEmptyAsync(TimeSpan.FromMilliseconds(200)).AsTask());
    }

    [Fact]
    public async Task WaitUntilEmptyAsync_ShouldNotResolveReaderFactory_WhenNoWorkerIsActive()
    {
        // No OutboxReaderFactory is registered
        OutboxTestingHelper helper = new(new ServiceCollection().BuildServiceProvider(), new LoggerSubstitute<OutboxTestingHelperTests>(LogLevel.Trace));

        (await helper.IsEmptyIgnoringErrorsAsync(null)).ShouldBeTrue();
    }

    private sealed record TestOutboxSettings(string Name) : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;

        public override string ToString() => Name;
    }

    private sealed class FakeLock : IDistributedLock
    {
        public ValueTask<DistributedLockHandle> AcquireAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<DistributedLockHandle>(new FakeLockHandle());
    }

    private sealed class FakeLockHandle : DistributedLockHandle
    {
        public override CancellationToken LockLostToken => CancellationToken.None;

        protected override void Dispose(bool disposing)
        {
        }

        protected override ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeOutboxReader : IOutboxReader
    {
        private readonly List<OutboxMessage> _messages;

        public FakeOutboxReader(int count)
        {
            _messages = [.. Enumerable.Range(0, count).Select(i => new OutboxMessage([(byte)i], null, "endpoint"))];
        }

        public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count)
        {
            DisposableAsyncEnumerable<OutboxMessage> enumerable = new(_messages.Take(count).ToList().ToAsyncEnumerable());
            return Task.FromResult<IDisposableAsyncEnumerable<OutboxMessage>>(enumerable);
        }

        public Task<int> GetLengthAsync() => Task.FromResult(_messages.Count);

        public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(TimeSpan.Zero);

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages)
        {
            foreach (OutboxMessage outboxMessage in outboxMessages.ToList())
            {
                _messages.Remove(outboxMessage);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private ServiceProvider? _serviceProvider;

        public Dictionary<OutboxSettings, IOutboxReader> Readers { get; } = [];

        public List<OutboxWorkerService> Services { get; } = [];

        private ProducerCollection Producers { get; } = [];

        public Fixture AddOutbox(OutboxSettings settings, int messagesCount)
        {
            Readers[settings] = new FakeOutboxReader(messagesCount);
            return this;
        }

        public IOutboxWorker AddWorker(OutboxSettings settings, TimeSpan? interval = null)
        {
            Readers.TryAdd(settings, new FakeOutboxReader(0));

            IOutboxWorker worker = Substitute.For<IOutboxWorker>();
            worker.GetLengthAsync().Returns(_ => Readers[settings].GetLengthAsync());

            Services.Add(
                new OutboxWorkerService(
                    new OutboxWorkerSettings(settings, null)
                    {
                        Interval = interval ?? TimeSpan.FromHours(1)
                    },
                    worker,
                    new FakeLock(),
                    new SilverbackLoggerSubstitute<OutboxWorkerService>()));

            return worker;
        }

        public void AddProducer(OutboxSettings settings)
        {
            Readers.TryAdd(settings, new FakeOutboxReader(0));

            IProducer producer = Substitute.For<IProducer>();
            producer.EndpointConfiguration.Returns(
                new TestProducerEndpointConfiguration("topic")
                {
                    Strategy = new OutboxProduceStrategy(settings)
                });

            Producers.Add(producer);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            Services.ForEach(service => service.Dispose());
        }

        public OutboxTestingHelper Build()
        {
            OutboxReaderFactory readerFactory = new();
            readerFactory.OverrideFactories((settings, _) => Readers[settings]);

            ServiceCollection services = new();
            services.AddSingleton(readerFactory);
            services.AddSingleton<IProducerCollection>(Producers);

            foreach (OutboxWorkerService service in Services)
            {
                services.AddSingleton<IHostedService>(service);
            }

            _serviceProvider = services.BuildServiceProvider();

            return new OutboxTestingHelper(_serviceProvider, new LoggerSubstitute<OutboxTestingHelperTests>(LogLevel.Trace));
        }
    }
}
