// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task StopAsync_ShouldNotWaitForStopInProgress_WhenProcessingRequestsStop(
        bool requestedBeforeCancellation,
        bool triggerReconnect)
    {
        StopRequestingBehavior behavior = new(triggerReconnect);
        using TestConsumer consumer = CreateConsumer(behavior);
        TaskCompletionSource<bool> stopCoreReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseStopCore = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.StopCoreCallback = () => stopCoreReached.TrySetResult(true);
        consumer.StopGate = releaseStopCore.Task;

        // The blocking stop waits for the processing, which requests the stop like the error policies do
        consumer.DrainGate = behavior.Completed.Task;
        await consumer.StartAsync();
        Task consuming = consumer.ConsumeAsync().AsTask();
        await behavior.Started.Task.WaitAsync(Timeout);

        Task stopping = consumer.StopAsync().AsTask();

        try
        {
            await stopCoreReached.Task.WaitAsync(Timeout);

            if (requestedBeforeCancellation)
            {
                // The stop holds the semaphore but didn't cancel the processing yet
                behavior.RequestStop.TrySetResult(true);
                await behavior.StopRequested.Task.WaitAsync(Timeout);
                releaseStopCore.TrySetResult(true);
            }
            else
            {
                releaseStopCore.TrySetResult(true);
                await behavior.Canceled.Task.WaitAsync(Timeout);
                behavior.RequestStop.TrySetResult(true);
            }

            await Task.WhenAll(stopping, consuming).WaitAsync(Timeout);

            behavior.IsStopRequestCompleted.ShouldBeTrue();
            consumer.StopRequests.ShouldHaveSingleItem().ShouldBeNull();
            consumer.Started.ShouldBeFalse();
            consumer.Stopping.ShouldBeFalse();
            await consumer.Client.DidNotReceive().ReconnectAsync();
        }
        finally
        {
            releaseStopCore.TrySetResult(true);
            behavior.RequestStop.TrySetResult(true);
            await Task.WhenAll(stopping, consuming).WaitAsync(Timeout);
        }
    }

    [Fact]
    public async Task TriggerReconnectAsync_ShouldNotReconnect_WhenStopping()
    {
        using TestConsumer consumer = CreateConsumer();
        TaskCompletionSource<bool> releaseDrain = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.DrainGate = releaseDrain.Task;
        await consumer.StartAsync();

        Task stopping = consumer.StopAsync().AsTask();

        try
        {
            // Triggered while stopping (e.g. by the Kafka poll timeout), the reconnect must neither wait for the stop nor restart the
            // consumer
            await consumer.TriggerReconnectAsync().AsTask().WaitAsync(Timeout);

            releaseDrain.TrySetResult(true);
            await stopping.WaitAsync(Timeout);

            consumer.StopRequests.ShouldHaveSingleItem().ShouldBeNull();
            consumer.Started.ShouldBeFalse();
            await consumer.Client.DidNotReceive().ReconnectAsync();
        }
        finally
        {
            releaseDrain.TrySetResult(true);
            await stopping.WaitAsync(Timeout);
        }
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task TriggerReconnectAsync_ShouldNotReconnect_WhenStopped()
    {
        using TestConsumer consumer = CreateConsumer();
        TaskCompletionSource<bool> reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.Client.ReconnectAsync().Returns(_ =>
        {
            reconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        await consumer.StartAsync();
        await consumer.StopAsync();

        // Block a pending reconnect while it waits for the consuming to stop, since any further request is ignored until then
        TaskCompletionSource<bool> releaseDrain = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.DrainGate = releaseDrain.Task;

        try
        {
            await consumer.TriggerReconnectAsync().AsTask().WaitAsync(Timeout);

            // The ignored request must not prevent reconnecting the consumer once it is started again
            await consumer.StartAsync();
            await consumer.TriggerReconnectAsync().AsTask().WaitAsync(Timeout);

            consumer.Started.ShouldBeFalse();

            releaseDrain.TrySetResult(true);
            await reconnected.Task.WaitAsync(Timeout);

            consumer.StopRequests.ShouldBe([null, null]);
            await consumer.Client.Received(1).ReconnectAsync();
        }
        finally
        {
            releaseDrain.TrySetResult(true);
        }
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

        public Task StopGate { get; set; } = Task.CompletedTask;

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

        protected override async ValueTask StopCoreAsync()
        {
            IsStopping.ShouldBeTrue();

            StopCalls++;
            StopCoreCallback?.Invoke();

            await StopGate;
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

    private sealed class StopRequestingBehavior(bool triggerReconnect) : IConsumerBehavior
    {
        private static readonly TimeSpan StopRequestTimeout = TimeSpan.FromSeconds(2);

        public int SortIndex => 0;

        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> RequestStop { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> StopRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsStopRequestCompleted { get; private set; }

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            try
            {
                await using CancellationTokenRegistration registration = cancellationToken.Register(() => Canceled.TrySetResult(true));
                Started.TrySetResult(true);
                await RequestStop.Task.WaitAsync(Timeout, CancellationToken.None);

                // Stop without waiting for the processing itself, as the error policies do when the processing fails, or reconnect, as
                // they do when the rollback fails
                Task stopRequest = triggerReconnect
                    ? context.Consumer.TriggerReconnectAsync().AsTask()
                    : context.Consumer.StopAsync(context.Envelope.BrokerMessageIdentifier, false).AsTask();
                StopRequested.TrySetResult(true);

                // Bounded, not to deadlock the test when the stop request waits for the stop in progress
                await Task.WhenAny(stopRequest, Task.Delay(StopRequestTimeout, CancellationToken.None));
                IsStopRequestCompleted = stopRequest.IsCompletedSuccessfully;
            }
            finally
            {
                context.Dispose();
                Completed.TrySetResult(true);
            }
        }
    }
}
