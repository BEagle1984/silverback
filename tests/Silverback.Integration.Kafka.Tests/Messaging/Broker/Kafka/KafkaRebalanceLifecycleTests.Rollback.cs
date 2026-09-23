// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public partial class KafkaRebalanceLifecycleTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The poll and rollback complete before the harness is disposed")]
    public async Task Rollback_ShouldDrainPendingPollBeforeSeekingAndReplaying(bool independent, bool autoCommit, bool beforeConsumeReturns)
    {
        await using PollHarness harness = new(independent, autoCommit: autoCommit);
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();

        if (!beforeConsumeReturns)
            harness.AfterConsume = BlockPoll;

        Task poll = harness.PollAsync(() =>
        {
            ConsumeResult<byte[]?, byte[]?> record = harness.Record(0, 5);

            if (beforeConsumeReturns)
                BlockPoll();

            return record;
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);

            // Seeking while a fetched record is still outstanding lets it enter the replacement before replay
            harness.Seeks.ShouldBeEmpty();
            harness.Resumes.ShouldBeEmpty();
        }
        finally
        {
            harness.AfterConsume = null;
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);
        await harness.DeliverAsync([.. Enumerable.Range(0, 6).Select(offset => (0, (long)offset))]);
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 6);
        harness.Client.Commit();

        _output.WriteLine($"Completed offsets after rollback: {string.Join(", ", harness.Completed.Select(delivery => delivery.Offset))}");

        harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 2L, 3L, 4L, 5L]);
        harness.CommittedOffset(0).ShouldBe(6);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();

        return;

        void BlockPoll()
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The pending poll is released before the harness is disposed")]
    public async Task Rollback_ShouldReplayFirstPolledRecord_WhenSharedPartitionPositionWasUnknown(bool autoCommit)
    {
        await using PollHarness harness = new(independent: false, autoCommit: autoCommit);
        await harness.StartAsync();
        await harness.PollAsync(() =>
        {
            harness.AssignOffsets(
            [
                new TopicPartitionOffset("topic", 0, Offset.Unset),
                new TopicPartitionOffset("topic", 1, Offset.Unset)
            ]);

            return null;
        });

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();
        Task poll = harness.PollAsync(() =>
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();

            return harness.Record(1, 0);
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);
        }
        finally
        {
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await PollHarness.WaitUntilAsync(() => harness.Resumes.Count == 2);

        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 0, 0));
        harness.Seeks.ShouldContain(new TopicPartitionOffset("topic", 1, 0));

        await harness.DeliverAsync((0, 0), (0, 1), (1, 0), (1, 1));
        await PollHarness.WaitUntilAsync(() => harness.StoredOffset(0) == 2 && harness.StoredOffset(1) == 2);
        harness.Client.Commit();

        harness.Completed.Select(delivery => (delivery.Partition, delivery.Offset)).ShouldBe([(0, 0L), (0, 1L), (1, 0L), (1, 1L)]);
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The pending poll is released before the harness is disposed")]
    public async Task Shutdown_ShouldCompleteAndDiscardRollback_WhenWaitingForPendingPoll(bool independent, bool reconnect)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0), (0, 1));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> recordPolled = NewSignal();
        TaskCompletionSource<bool> releasePoll = NewSignal();
        Task poll = harness.PollAsync(() =>
        {
            recordPolled.TrySetResult(true);
            releasePoll.Task.WaitAsync(Timeout).GetAwaiter().GetResult();

            return harness.Record(0, 5);
        });

        try
        {
            await recordPolled.Task.WaitAsync(Timeout);
            await harness.Consumer.RollbackAsync(active.Context!.Envelope.BrokerMessageIdentifier);
            await active.ReaderStopped.Task.WaitAsync(Timeout);
            await harness.Consumer.StopAsync(false).AsTask().WaitAsync(Timeout);

            harness.PollingStopped.IsCompleted.ShouldBeFalse();
            harness.Seeks.ShouldBeEmpty();
            harness.Resumes.ShouldBeEmpty();
        }
        finally
        {
            active.Release.TrySetResult(true);
            releasePoll.TrySetResult(true);
            await poll.WaitAsync(Timeout);
        }

        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.RollbackRestartObserved.Task.WaitAsync(Timeout);

        harness.GetChannel(0).ShouldBeNull();
        harness.Completed.ShouldBeEmpty();

        if (reconnect)
        {
            await harness.StartAsync(0);
            await harness.DeliverAsync([.. Enumerable.Range(0, 6).Select(offset => (0, (long)offset))]);
            await harness.WaitForCommitAsync(0, 6);

            harness.Completed.Select(delivery => delivery.Offset).ShouldBe([0L, 1L, 2L, 3L, 4L, 5L]);
        }

        harness.Seeks.ShouldBeEmpty();
        harness.Resumes.ShouldBeEmpty();
        harness.UnsafeStores.ShouldBeEmpty();
        harness.UnsafeCommits.ShouldBeEmpty();
        harness.Errors.ShouldBeEmpty();
    }
}
