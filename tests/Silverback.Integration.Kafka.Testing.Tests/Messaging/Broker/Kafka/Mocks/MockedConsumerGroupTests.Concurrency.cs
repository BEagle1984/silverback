// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public partial class MockedConsumerGroupTests
{
    private static readonly TimeSpan RaceTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAssignment_ShouldReturnEmpty_WhenConsumerHasNoGroupAssignment(bool removed)
    {
        await using GroupRaceFixture fixture = new();
        MockedConfluentConsumer consumer = fixture.CreateConsumer();

        if (removed)
        {
            fixture.Group.Assign(consumer, [new TopicPartition("topic", 0)]);
            fixture.Group.Remove(consumer);
        }

        fixture.Group.GetAssignment(consumer).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PartitionAssignmentStrategy.Range)]
    [InlineData(PartitionAssignmentStrategy.CooperativeSticky)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks complete before the fixture is disposed")]
    public async Task GetAssignment_ShouldReturnStableSnapshot_WhenRebalanceChangesPartitions(PartitionAssignmentStrategy strategy)
    {
        await using GroupRaceFixture fixture = new();
        MockedConfluentConsumer first = fixture.CreateConsumer(strategy);
        first.Subscribe("topic");
        await fixture.AssignAllAsync();

        IReadOnlyCollection<TopicPartition> assignment = fixture.Group.GetAssignment(first);
        TopicPartition[] original = [.. assignment];
        original.Length.ShouldBe(4);

        first.PartitionsRevokedHandler = (_, partitions) =>
        {
            // A callback must also permit group reads from another thread
            Task.Run(() => fixture.Group.GetAssignment(first)).Wait(RaceTimeout).ShouldBeTrue();

            return partitions;
        };

        MockedConfluentConsumer joining = fixture.CreateConsumer(strategy);
        joining.Subscribe("topic");
        await fixture.AssignAllAsync();

        fixture.Group.GetAssignment(first).Count.ShouldBe(2);
        fixture.Group.GetAssignment(joining).Count.ShouldBe(2);
        assignment.ShouldBe(original);
    }

    [Theory]
    [InlineData(PartitionAssignmentStrategy.Range)]
    [InlineData(PartitionAssignmentStrategy.CooperativeSticky)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Callbacks and tasks complete before the gates and fixture are disposed")]
    public async Task EnsurePartitionsAssigned_ShouldDiscardAssignment_WhenConsumerLeavesDuringCallback(PartitionAssignmentStrategy strategy)
    {
        await using GroupRaceFixture fixture = new();
        MockedConfluentConsumer consumer = fixture.CreateConsumer(strategy);
        TaskCompletionSource callbackStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseCallback = new();

        consumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            callbackStarted.TrySetResult();
            releaseCallback.Wait(RaceTimeout).ShouldBeTrue();

            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };

        consumer.Subscribe("topic");
        await fixture.WaitUntilAssignmentCanStartAsync();
        Task<bool> assignment = Task.Run(() => consumer.EnsurePartitionsAssigned(CancellationToken.None));

        try
        {
            await callbackStarted.Task.WaitAsync(RaceTimeout);
            await Task.Run(() => fixture.Group.Remove(consumer)).WaitAsync(RaceTimeout);
        }
        finally
        {
            releaseCallback.Set();
        }

        (await assignment.WaitAsync(RaceTimeout)).ShouldBeFalse();
        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The blocked read and removal complete before the gates and fixture are disposed")]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldTolerateRemoval_DuringConsumerInspection()
    {
        await using GroupRaceFixture fixture = new();
        MockedConfluentConsumer first = fixture.CreateConsumer();
        MockedConfluentConsumer leaving = fixture.CreateConsumer();
        first.Subscribe("topic");
        leaving.Subscribe("topic");
        await fixture.AssignAllAsync();

        TaskCompletionSource inspectionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseInspection = new();
        int inspected = 0;
        fixture.BeforeTopicInspection = (_, _) =>
        {
            if (Interlocked.Exchange(ref inspected, 1) != 0)
                return;

            inspectionStarted.TrySetResult();
            releaseInspection.Wait(RaceTimeout).ShouldBeTrue();
        };

        using CancellationTokenSource timeout = new(RaceTimeout);
        Task inspection = Task.Run(async () => await fixture.Group.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token));

        try
        {
            await inspectionStarted.Task.WaitAsync(RaceTimeout);
            await Task.Run(leaving.Dispose).WaitAsync(RaceTimeout);
        }
        finally
        {
            releaseInspection.Set();
        }

        await fixture.AssignAllAsync();
        await inspection.WaitAsync(RaceTimeout);

        fixture.Group.GetAssignment(first).Count.ShouldBe(4);
        fixture.Group.GetAssignment(leaving).ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "All tasks and callbacks complete before the gates and fixture are disposed")]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldRecheckNewConsumer_WhenMembershipChangesDuringInspection()
    {
        await using GroupRaceFixture fixture = new();
        MockedConfluentConsumer first = fixture.CreateConsumer();
        first.Subscribe("topic");
        await fixture.AssignAllAsync();

        TaskCompletionSource inspectionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource joiningConsumerInspected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseInspection = new();
        int inspected = 0;
        fixture.BeforeTopicInspection = (_, _) =>
        {
            if (Interlocked.Exchange(ref inspected, 1) != 0)
                return;

            inspectionStarted.TrySetResult();
            releaseInspection.Wait(RaceTimeout).ShouldBeTrue();
        };

        using CancellationTokenSource timeout = new(RaceTimeout);
        Task inspection = Task.Run(async () => await fixture.Group.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token));

        try
        {
            await inspectionStarted.Task.WaitAsync(RaceTimeout);
            MockedConfluentConsumer joining = fixture.CreateConsumer();
            fixture.Produce("joining-topic");
            joining.Subscribe("joining-topic");
            await fixture.AssignAllAsync();

            fixture.BeforeTopicInspection = (topic, _) =>
            {
                if (topic == "joining-topic")
                    joiningConsumerInspected.TrySetResult();
            };
        }
        finally
        {
            releaseInspection.Set();
        }

        Task completed = await Task.WhenAny(inspection, joiningConsumerInspected.Task).WaitAsync(RaceTimeout);
        completed.ShouldBeSameAs(joiningConsumerInspected.Task);
        inspection.IsCompleted.ShouldBeFalse();

        fixture.Group.Commit([new TopicPartitionOffset("joining-topic", 0, 1)]);
        await inspection.WaitAsync(RaceTimeout);
    }

    private sealed class GroupRaceFixture : IAsyncDisposable
    {
        private readonly MockedKafkaOptions _options = new() { PartitionsAssignmentDelay = TimeSpan.Zero };

        private readonly IInMemoryTopicCollection _topics = Substitute.For<IInMemoryTopicCollection>();

        private readonly List<MockedConfluentConsumer> _consumers = [];

        public GroupRaceFixture()
        {
            _options.TopicPartitionsCount.Add("topic", 4);
            InMemoryTopicCollection topics = new(_options);
            _topics.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(call => topics.Get(call.ArgAt<string>(0), call.ArgAt<string>(1)));

            _topics.Get(Arg.Any<string>(), Arg.Any<ClientConfig>()).Returns(call =>
            {
                BeforeTopicInspection?.Invoke(call.ArgAt<string>(0), call.ArgAt<ClientConfig>(1));

                return topics.Get(call.ArgAt<string>(0), call.ArgAt<ClientConfig>(1));
            });

            Group = new MockedConsumerGroup("group", BootstrapServers, _topics);
        }

        public MockedConsumerGroup Group { get; }

        public Action<string, ClientConfig>? BeforeTopicInspection { get; set; }

        public MockedConfluentConsumer CreateConsumer(PartitionAssignmentStrategy strategy = PartitionAssignmentStrategy.Range)
        {
            MockedConfluentConsumer consumer = GetConsumer(Group, _topics, _options, strategy);
            _consumers.Add(consumer);

            return consumer;
        }

        public void Produce(string topic) =>
            _topics.Get(topic, BootstrapServers).Push(0, new Message<byte[]?, byte[]?> { Value = [1] }, Guid.Empty);

        public async Task AssignAllAsync()
        {
            using CancellationTokenSource timeout = new(RaceTimeout);

            while (Group.IsRebalancing || Group.IsRebalanceScheduled ||
                   _consumers.Where(consumer => !consumer.IsDisposed && consumer.Subscription.Count > 0)
                       .Any(consumer => !consumer.EnsurePartitionsAssigned(timeout.Token)))
            {
                await Task.Delay(1, timeout.Token);
            }
        }

        public async Task WaitUntilAssignmentCanStartAsync()
        {
            using CancellationTokenSource timeout = new(RaceTimeout);

            while (Group.IsRebalancing || Group.IsRebalanceScheduled)
            {
                await Task.Delay(1, timeout.Token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            using CancellationTokenSource timeout = new(RaceTimeout);

            while (Group.IsRebalanceScheduled)
            {
                await Task.Delay(1, timeout.Token);
            }

            // Await the background rebalance before disposing its synchronization primitive
            SemaphoreSlim semaphore = (SemaphoreSlim)typeof(MockedConsumerGroup)
                .GetField("_subscriptionsChangeSemaphore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Group)!;
            await semaphore.WaitAsync(timeout.Token);
            semaphore.Release();
            Group.Dispose();
        }
    }
}
