// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback and polling stop are awaited before the harness is disposed")]
    public async Task CooperativeRebalance_ShouldPreserveRetainedBufferOrder_WhenCanceledPollReturnsAnotherRecord(bool autoCommit)
    {
        await using PollHarness harness = new(true, true, backpressure: 2, autoCommit: autoCommit);
        await harness.StartAsync(0, 1);

        PartitionChannel retained = harness.GetChannel(1)!;
        ProcessingGate active = harness.Block(1, 0);
        await harness.DeliverAsync((1, 0));
        await active.Started.Task.WaitAsync(Timeout);
        await harness.DeliverAsync((1, 1), (1, 2));

        Task pollingStopped = Task.CompletedTask;
        await harness.PollAsync(() =>
        {
            // Force the canceled-poll boundary that the old cooperative rebalance path reached
            pollingStopped = harness.StopPollingAsync();
            harness.Revoke(0);

            return harness.Record(1, 3);
        });

        // The returned record must reach overflow before the retained reader is released
        await pollingStopped.WaitAsync(Timeout);

        harness.GetChannel(1).ShouldBeSameAs(retained);
        harness.GetChannel(0).ShouldBeNull();
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        retained.ReadCancellationToken.IsCancellationRequested.ShouldBeFalse();
        active.ReaderStopped.Task.IsCompleted.ShouldBeFalse();
        harness.Completed.ShouldBeEmpty();
        harness.CommittedOffset(1).ShouldBe(0);

        active.Release.TrySetResult(true);
        await PollHarness.WaitUntilAsync(() => harness.Completed.Count == 4);

        _output.WriteLine($"Retained partition order: {string.Join(", ", harness.Completed.Select(delivery => delivery.Offset))}");

        harness.Completed.ShouldBe([new Delivery(1, 0, 1), new Delivery(1, 1, 1), new Delivery(1, 2, 1), new Delivery(1, 3, 1)]);

        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 4);
        harness.Client.Commit();

        harness.CommittedOffset(1).ShouldBe(4);
        harness.Seeks.ShouldBeEmpty();
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The rebalance callback is awaited before the harness is disposed")]
    public async Task CooperativeRebalance_ShouldDrainRetainedHandlerBeforeReplay_WhenSharingOneChannel(bool autoCommit)
    {
        await using PollHarness harness = new(false, true, backpressure: 2, parallelism: 1, autoCommit: autoCommit);
        await harness.StartAsync(0, 1);
        await harness.DeliverAsync((1, 0));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 1);
        harness.Client.Commit();

        PartitionChannel original = harness.GetChannel(1)!;
        ProcessingGate active = harness.Block(1, 1);
        active.IgnoreCancellation = true;
        await harness.DeliverAsync((1, 1), (1, 2), (1, 3));
        await active.Started.Task.WaitAsync(Timeout);

        Task rebalance = harness.PollAsync(() =>
        {
            harness.Revoke(0);

            return null;
        });

        await active.ReaderStopped.Task.WaitAsync(Timeout);

        rebalance.IsCompleted.ShouldBeFalse();
        harness.GetChannel(1).ShouldBeSameAs(original);
        harness.StoredOffset(1).ShouldBe(1, "The active retained record has not completed");

        active.Release.TrySetResult(true);
        await rebalance;

        PartitionChannel replacement = harness.GetChannel(1)!;
        KafkaOffset originalOffset = (KafkaOffset)active.Context!.Envelope.BrokerMessageIdentifier;

        replacement.ShouldNotBeSameAs(original);
        originalOffset.BelongsToChannel(replacement.InstanceId).ShouldBeFalse();
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 1)]);
        harness.StoredOffset(1).ShouldBe(1, "Retiring retained work must replay instead of committing after rollback begins");

        harness.Client.ClearReceivedCalls();
        await harness.Consumer.CommitAsync(originalOffset);

        harness.Client.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());

        ProcessingGate replay = harness.Block(1, 1);
        await harness.DeliverAsync((1, 1), (1, 2), (1, 3));
        await replay.Started.Task.WaitAsync(Timeout);

        ((KafkaOffset)replay.Context!.Envelope.BrokerMessageIdentifier).BelongsToChannel(replacement.InstanceId).ShouldBeTrue();

        replay.Release.TrySetResult(true);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 4);
        harness.Client.Commit();

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 1L, 2L, 3L]);
        harness.Completed.ShouldAllBe(delivery => delivery.Partition == 1 && delivery.Epoch == 1);
        harness.CommittedOffset(1).ShouldBe(4);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CooperativeRebalance_ShouldReplayRetainedPartialBatchWithoutAdvancingItsCommit(bool autoCommit)
    {
        await using PollHarness harness = new(false, true, backpressure: 2, parallelism: 1, autoCommit: autoCommit, batchPipeline: true);
        await harness.StartAsync(0, 1);
        await harness.DeliverAsync([.. Enumerable.Range(0, 10).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 10);
        harness.Client.Commit();

        PartitionChannel original = harness.GetChannel(1)!;
        await harness.DeliverAsync([.. Enumerable.Range(10, 5).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.Completed.Count == 15);

        await harness.PollAsync(() =>
        {
            harness.Revoke(0);

            return null;
        });

        harness.GetChannel(1).ShouldNotBeSameAs(original);
        harness.Client.Assignment.ShouldBe([new TopicPartition("topic", 1)]);
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 10)]);
        harness.StoredOffset(1).ShouldBe(10);
        harness.CommittedOffset(1).ShouldBe(10);
        harness.CompletedBatchSizes.ShouldBe([10]);
        harness.Completed.Select(delivery => delivery.Offset).ShouldBe(Enumerable.Range(0, 15).Select(offset => (long)offset));

        await harness.DeliverAsync([.. Enumerable.Range(10, 10).Select(offset => (1, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(1) == 20);
        harness.Client.Commit();

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe(Enumerable.Range(0, 15).Concat(Enumerable.Range(10, 10)).Select(offset => (long)offset));
        harness.Completed.Select(delivery => delivery.Offset).Distinct().ShouldBe(Enumerable.Range(0, 20).Select(offset => (long)offset));
        harness.Completed.ShouldAllBe(delivery => delivery.Partition == 1 && delivery.Epoch == 1);
        harness.CompletedBatchSizes.ShouldBe([10, 10]);
        harness.CommittedOffset(1).ShouldBe(20);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }
}
