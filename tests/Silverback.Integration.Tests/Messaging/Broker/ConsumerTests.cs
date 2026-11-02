// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_ShouldValidateIdentifierAfterPendingStart(bool replaceIdentifier)
    {
        using TestConsumer consumer = CreateConsumer();
        TestOffset originalIdentifier = consumer.CurrentIdentifier;
        TaskCompletionSource<bool> releaseStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.StartGate = releaseStart.Task;

        Task starting = consumer.StartAsync().AsTask();
        Task stopping = consumer.StopAsync(originalIdentifier, false).AsTask();

        try
        {
            consumer.StopRequests.ShouldBeEmpty();
            stopping.IsCompleted.ShouldBeFalse();

            if (replaceIdentifier)
                consumer.CurrentIdentifier = new TestOffset();

            releaseStart.TrySetResult(true);
            await Task.WhenAll(starting, stopping).WaitAsync(Timeout);

            consumer.StopRequests.ShouldHaveSingleItem().ShouldBeSameAs(originalIdentifier);
            consumer.Started.ShouldBe(replaceIdentifier);
            consumer.Stopping.ShouldBeFalse();
            consumer.StopCalls.ShouldBe(replaceIdentifier ? 0 : 1);

            if (replaceIdentifier)
            {
                // A rejected obsolete request must release the semaphore and allow the current request to stop
                await consumer.StopAsync(consumer.CurrentIdentifier, false).AsTask().WaitAsync(Timeout);

                consumer.Started.ShouldBeFalse();
                consumer.StopCalls.ShouldBe(1);
            }
        }
        finally
        {
            releaseStart.TrySetResult(true);
            await Task.WhenAll(starting, stopping).WaitAsync(Timeout);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task StopAsync_ShouldHonorWaitUntilStopped(bool identified, bool waitUntilStopped)
    {
        using TestConsumer consumer = CreateConsumer();
        TaskCompletionSource<bool> releaseDrain = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.DrainGate = releaseDrain.Task;
        await consumer.StartAsync();

        Task stopping = identified
            ? consumer.StopAsync(consumer.CurrentIdentifier, waitUntilStopped).AsTask()
            : consumer.StopAsync(waitUntilStopped).AsTask();

        try
        {
            consumer.StopRequests.ShouldHaveSingleItem().ShouldBeSameAs(identified ? consumer.CurrentIdentifier : null);
            consumer.StopCalls.ShouldBe(1);
            consumer.DrainCalls.ShouldBe(waitUntilStopped ? 1 : 0);
            stopping.IsCompleted.ShouldBe(!waitUntilStopped);

            releaseDrain.TrySetResult(true);
            await stopping.WaitAsync(Timeout);

            consumer.Started.ShouldBeFalse();
            consumer.Stopping.ShouldBeFalse();
        }
        finally
        {
            releaseDrain.TrySetResult(true);
            await stopping.WaitAsync(Timeout);
        }
    }

    [Fact]
    public async Task StopAsync_ShouldStopReadingBeforeCancelingProcessing()
    {
        CancellationTokenCapturingBehavior behavior = new();
        using TestConsumer consumer = CreateConsumer(behavior);
        bool? isCanceledWhenStoppingReading = null;
        consumer.StopCoreCallback = () => isCanceledWhenStoppingReading = behavior.CancellationToken.IsCancellationRequested;
        await consumer.StartAsync();
        await consumer.ConsumeAsync();

        await consumer.StopAsync().AsTask().WaitAsync(Timeout);

        // A subscriber finishing as soon as its token is canceled must not let the reader start the next message
        isCanceledWhenStoppingReading.ShouldBe(false);
        behavior.CancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    private static TestConsumer CreateConsumer(params IConsumerBehavior[] behaviors)
    {
        IBrokerClient client = Substitute.For<IBrokerClient>();
        client.Initialized.Returns(new AsyncEvent<BrokerClient>());
        client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        client.Status.Returns(ClientStatus.Initialized);

        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
        behaviorsProvider.GetBehaviorsList().Returns(behaviors);

        return new TestConsumer(client, behaviorsProvider);
    }

    private sealed class TestConsumer(
        IBrokerClient client,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviors)
        : Consumer<TestOffset>("test", client, [], behaviors, Substitute.For<IServiceProvider>(), Substitute.For<ISilverbackLogger<IConsumer>>())
    {
        public List<TestOffset?> StopRequests { get; } = [];

        public bool Started => IsStarted;

        public bool Stopping => IsStopping;

        public TestOffset CurrentIdentifier { get; set; } = new();

        public Task StartGate { get; set; } = Task.CompletedTask;

        public Task DrainGate { get; set; } = Task.CompletedTask;

        public int StopCalls { get; private set; }

        public int DrainCalls { get; private set; }

        public Action? StopCoreCallback { get; set; }

        public ValueTask ConsumeAsync() =>
            HandleMessageAsync([], [], TestConsumerEndpoint.GetDefault(), CurrentIdentifier, Substitute.For<ISequenceStore>());

        protected override async ValueTask StartCoreAsync() => await StartGate;

        protected override bool TryBeginStop(TestOffset? brokerMessageIdentifier)
        {
            StopRequests.Add(brokerMessageIdentifier);

            return (brokerMessageIdentifier == null || ReferenceEquals(brokerMessageIdentifier, CurrentIdentifier)) &&
                   base.TryBeginStop(brokerMessageIdentifier);
        }

        protected override ValueTask StopCoreAsync()
        {
            IsStopping.ShouldBeTrue();

            StopCalls++;
            StopCoreCallback?.Invoke();

            return ValueTask.CompletedTask;
        }

        protected override async ValueTask WaitUntilConsumingStoppedCoreAsync()
        {
            DrainCalls++;
            await DrainGate;
        }

        protected override ValueTask CommitCoreAsync(IReadOnlyCollection<TestOffset> brokerMessageIdentifiers) =>
            ValueTask.CompletedTask;

        protected override ValueTask RollbackCoreAsync(IReadOnlyCollection<TestOffset> brokerMessageIdentifiers) =>
            ValueTask.CompletedTask;
    }

    private sealed class CancellationTokenCapturingBehavior : IConsumerBehavior
    {
        public int SortIndex => 0;

        public CancellationToken CancellationToken { get; private set; }

        public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            context.Dispose();
            CancellationToken = cancellationToken;

            return ValueTask.CompletedTask;
        }
    }
}
