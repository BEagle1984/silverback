// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(5L, new long[] { 4, 3, 4 }, 3L)]
    [InlineData(5L, new long[] { 7 }, 5L)]
    [InlineData(-1L, new long[] { 3 }, 3L)]
    public async Task Rollback_ShouldSeekToLowestRequestedOffset_WhenRolledBackAgainWhileRestarting(
        long rollbackOffset,
        long[] laterRollbackOffsets,
        long expectedSeekOffset)
    {
        await using LifecycleHarness harness = new();
        TaskCompletionSource<bool> processing = NewSignal();
        TaskCompletionSource<bool> release = NewSignal();
        harness.OnMessage = async (offset, _) =>
        {
            if (offset.Offset != 5)
                return;

            processing.TrySetResult(true);
            await release.Task.WaitAsync(Timeout, CancellationToken.None);
        };

        await harness.StartAsync(0);
        await harness.DeliverAsync((0, 3), (0, 4), (0, 5));
        await processing.Task.WaitAsync(Timeout);

        try
        {
            // The restart waits for the record being processed
            await harness.Consumer.RollbackAsync(GetOffset(rollbackOffset));

            foreach (long laterRollbackOffset in laterRollbackOffsets)
            {
                await harness.Consumer.RollbackAsync(GetOffset(laterRollbackOffset));
            }

            // Ignored: a special offset, an offset read by another channel instance and one of a partition that isn't rolled back
            await harness.Consumer.RollbackAsync(new KafkaOffset(LifecycleHarness.ToTopicPartition(0), Offset.Unset));
            await harness.Consumer.RollbackAsync(new KafkaOffset(new TopicPartitionOffset("topic", 0, 1), Guid.NewGuid()));
            await harness.Consumer.RollbackAsync(new KafkaOffset("topic", 1, 0));

            harness.Seeks.ShouldBeEmpty();
        }
        finally
        {
            release.TrySetResult(true);
        }

        await LifecycleHarness.WaitUntilAsync(() => !harness.Resumes.IsEmpty);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, expectedSeekOffset)]);
        harness.Resumes.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);

        return;

        KafkaOffset GetOffset(long offset) =>
            offset < 0
                ? new KafkaOffset(LifecycleHarness.ToTopicPartition(0), Offset.Unset)
                : harness.Handled.FirstOrDefault(handledOffset => handledOffset.Offset == offset) ?? new KafkaOffset("topic", 0, offset);
    }

    [Theory]
    [InlineData(false, 5L)]
    [InlineData(true, 4L)]
    public async Task Rollback_ShouldIgnoreLowerOffsetRequestedForPreviousAssignment_WhenRolledBackAfterRestart(
        bool rollBackLowerOffsetAgain,
        long expectedSeekOffset)
    {
        await using LifecycleHarness harness = new();
        TaskCompletionSource<bool> firstProcessing = NewSignal();
        TaskCompletionSource<bool> firstRelease = NewSignal();
        TaskCompletionSource<bool> secondProcessing = NewSignal();
        TaskCompletionSource<bool> secondRelease = NewSignal();
        int deliveries = 0;
        harness.OnMessage = async (offset, _) =>
        {
            if (offset.Offset != 5)
                return;

            if (Interlocked.Increment(ref deliveries) == 1)
            {
                firstProcessing.TrySetResult(true);
                await firstRelease.Task.WaitAsync(Timeout, CancellationToken.None);
            }
            else
            {
                secondProcessing.TrySetResult(true);
                await secondRelease.Task.WaitAsync(Timeout, CancellationToken.None);
            }
        };

        await harness.StartAsync(0);
        await harness.DeliverAsync((0, 3), (0, 5));
        await firstProcessing.Task.WaitAsync(Timeout);
        KafkaOffset[] firstHandled = [.. harness.Handled];
        Task stopping;

        try
        {
            await harness.Consumer.RollbackAsync(firstHandled[1]);
            await harness.Consumer.RollbackAsync(firstHandled[0]);

            // The stop defeats the pending rollback, whose lower offset is left behind
            stopping = harness.Consumer.StopAsync().AsTask();
        }
        finally
        {
            firstRelease.TrySetResult(true);
        }

        await stopping.WaitAsync(Timeout);
        await LifecycleHarness.WaitUntilAsync(() => harness.Logs.Count(log => log.StartsWith("Skipping rollback restart", StringComparison.Ordinal)) == 2);

        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);

        await harness.DeliverAsync((0, 4), (0, 5));
        await secondProcessing.Task.WaitAsync(Timeout);
        KafkaOffset[] secondHandled = [.. harness.Handled.Skip(2)];

        try
        {
            await harness.Consumer.RollbackAsync(secondHandled[1]);

            if (rollBackLowerOffsetAgain)
                await harness.Consumer.RollbackAsync(secondHandled[0]);
        }
        finally
        {
            secondRelease.TrySetResult(true);
        }

        await LifecycleHarness.WaitUntilAsync(() => harness.Seeks.Count == 2);

        // The lower offset requested before the restart (3) doesn't apply to the current assignment
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0), new TopicPartitionOffset("topic", 0, expectedSeekOffset)]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The offsets are enumerated before the harness is disposed")]
    public async Task Rollback_ShouldBeSkipped_WhenClientStartsDisconnectingWhileOffsetsAreEnumerated()
    {
        await using LifecycleHarness harness = new();
        harness.OnMessage = (_, _) => Task.CompletedTask;
        await harness.StartAsync(0);

        await harness.DeliverAsync((0, 0));
        await LifecycleHarness.WaitUntilAsync(() => harness.Handled.Count == 1);
        KafkaOffset offset = harness.Handled.ShouldHaveSingleItem();

        await harness.Consumer.RollbackCoreForTestAsync(
            new BoundaryOffsets(offset, () => harness.Status = ClientStatus.Disconnecting, null));

        harness.HasLog("Skipping rollback restart").ShouldBeTrue();
        harness.Pauses.ShouldBeEmpty();
        harness.Seeks.ShouldBeEmpty();
    }
}
