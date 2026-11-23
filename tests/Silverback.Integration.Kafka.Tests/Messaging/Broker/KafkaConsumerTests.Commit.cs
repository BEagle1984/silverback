// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    private const string RollbackRestartSkippedLog = "Skipping rollback restart";

    [Fact]
    public async Task CommitAsync_ShouldCommitEveryCommitOffsetEachOffsets()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { CommitOffsetEach = 3 });
        await harness.StartAsync(0);

        await harness.DeliverAsync([.. Enumerable.Range(0, 7).Select(offset => (0, (long)offset))]);
        await LifecycleHarness.WaitUntilAsync(() => harness.HandlerCommits.Count == 7);

        harness.Stored.Select(offset => offset.Offset.Value).ShouldBe([1L, 2L, 3L, 4L, 5L, 6L, 7L]);
        harness.Client.Received(2).Commit();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetStorableOffsets_ShouldReturnCommittableOffsetsAndOptionallyRevokedOnes(bool includeRevokedPartitions)
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0, 1);
        await harness.DeliverAsync((0, 0), (0, 1));
        await harness.WaitForCommitAsync(0, 2);
        await harness.PollAsync(() =>
        {
            harness.Revoke(1);

            return null;
        });

        IReadOnlyCollection<KafkaOffset> storable = harness.Consumer.GetStorableOffsets(
            [
                new KafkaOffset("topic", 0, 0), // Lower than the committed one
                new KafkaOffset("topic", 0, 1), // Committed already, can be committed again
                new KafkaOffset("topic", 0, 5),
                new KafkaOffset("topic", 1, 3), // Revoked
                new KafkaOffset("topic", 2, 0) // Never assigned
            ],
            includeRevokedPartitions);

        storable.ShouldBe(
            includeRevokedPartitions
                ? [new KafkaOffset("topic", 0, 1), new KafkaOffset("topic", 0, 5), new KafkaOffset("topic", 1, 3)]
                : [new KafkaOffset("topic", 0, 1), new KafkaOffset("topic", 0, 5)]);
        harness.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(new long[] { 0, 1 }, new long[] { 0, 1 }, 1L, 2L)]
    [InlineData(new long[] { 0, 1 }, new long[] { 0, 1 }, -1L, 0L)]
    [InlineData(new long[] { 0 }, new long[] { 1 }, 1L, 1L)]
    public async Task Rollback_ShouldSkipOnlyRecordsStoredBeforeFailedCommit_WhenClientSideOffsetStoreIsUsed(
        long[] failedCommitOffsets,
        long[] rolledBackOffsets,
        long storedOffset,
        long expectedSeekOffset)
    {
        await using LifecycleHarness harness = new(configuration => configuration with { ClientSideOffsetStore = new InMemoryKafkaOffsetStoreSettings() });
        harness.OnMessage = (_, _) => Task.CompletedTask;
        await harness.StartAsync(0);

        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await LifecycleHarness.WaitUntilAsync(() => harness.Handled.Count == 3);
        KafkaOffset[] handled = [.. harness.Handled];

        // The offsets are stored to the client side offset store (by the subscriber, together with its changes) before committing
        if (storedOffset >= 0)
            harness.SetStoredOffsets(new KafkaOffset("topic", 0, storedOffset));

        harness.BeforeStore = _ => throw new KafkaException(ErrorCode.Local_State);

        await Should.ThrowAsync<KafkaException>(() => harness.Consumer.CommitAsync([.. failedCommitOffsets.Select(offset => handled[offset])]).AsTask());

        harness.BeforeStore = null;

        await harness.Consumer.RollbackAsync([.. rolledBackOffsets.Select(offset => handled[offset])]);
        await LifecycleHarness.WaitUntilAsync(() => !harness.Resumes.IsEmpty);

        // The records whose failed commit was stored are processed already, unlike the ones stored by a later failed processing
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, expectedSeekOffset)]);
        harness.Resumes.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
        harness.Paused.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The restart is awaited before the harness is disposed")]
    public async Task Rollback_ShouldNotSeekToOffsetOfStoppedChannel_WhenRolledBackWhileRestartLoadsStoredOffsets()
    {
        await using LifecycleHarness harness = new(configuration => configuration with { ClientSideOffsetStore = new InMemoryKafkaOffsetStoreSettings() });
        harness.OnMessage = (_, _) => Task.CompletedTask;
        await harness.StartAsync(0);

        await harness.DeliverAsync((0, 0), (0, 1), (0, 2));
        await LifecycleHarness.WaitUntilAsync(() => harness.Handled.Count == 3);
        KafkaOffset[] handled = [.. harness.Handled];

        harness.BeforeStore = _ => throw new KafkaException(ErrorCode.Local_State);
        await Should.ThrowAsync<KafkaException>(() => harness.Consumer.CommitAsync(handled[2]).AsTask());
        harness.BeforeStore = null;

        // Requested once the rolled back channel is stopped and removed, while the restart loads the stored offsets
        Task lateRollback = Task.CompletedTask;
        harness.BeforeGetStore = () =>
        {
            harness.BeforeGetStore = null;
            lateRollback = harness.Consumer.RollbackAsync(handled[0]).AsTask();
        };

        await harness.Consumer.RollbackAsync(handled[2]);
        await LifecycleHarness.WaitUntilAsync(() => !harness.Resumes.IsEmpty);
        await lateRollback.WaitAsync(Timeout);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 2)]);
        harness.Resumes.ShouldBe([LifecycleHarness.ToTopicPartition(0)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The restart is awaited before the harness is disposed")]
    public async Task Rollback_ShouldTriggerReconnectOnlyIfStillCurrent_WhenLoadingStoredOffsetsFails(bool revokedMeanwhile)
    {
        await using LifecycleHarness harness = new(configuration => configuration with { ClientSideOffsetStore = new InMemoryKafkaOffsetStoreSettings() });
        harness.OnMessage = (_, _) => Task.CompletedTask;
        await harness.StartAsync(0);

        await harness.DeliverAsync((0, 0));
        await LifecycleHarness.WaitUntilAsync(() => harness.Handled.Count == 1);
        KafkaOffset offset = harness.Handled.ShouldHaveSingleItem();

        harness.BeforeStore = _ => throw new KafkaException(ErrorCode.Local_State);
        await Should.ThrowAsync<KafkaException>(() => harness.Consumer.CommitAsync(offset).AsTask());
        harness.BeforeStore = null;

        TaskCompletionSource<bool> reconnected = NewSignal();
        harness.Client.ReconnectAsync().Returns(_ =>
        {
            reconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        TaskCompletionSource<bool> loadFailed = NewSignal();
        harness.BeforeGetStore = () =>
        {
            harness.BeforeGetStore = null;

            if (revokedMeanwhile)
                harness.Revoke(0);

            loadFailed.TrySetResult(true);

            throw new InvalidOperationException("The offset store is unavailable.");
        };

        harness.HasLog(RollbackRestartSkippedLog).ShouldBeFalse();

        await harness.Consumer.RollbackAsync(offset);
        await loadFailed.Task.WaitAsync(Timeout);

        if (revokedMeanwhile)
        {
            // Logged by the restart's catch block after deciding not to reconnect, the last step of the skipped restart
            await LifecycleHarness.WaitUntilAsync(() => harness.HasLog(RollbackRestartSkippedLog));

            harness.HasLog("Error occurred (re)starting").ShouldBeFalse();
            _ = harness.Client.DidNotReceive().ReconnectAsync();
        }
        else
        {
            await reconnected.Task.WaitAsync(Timeout);

            harness.HasLog("Error occurred (re)starting").ShouldBeTrue();
            harness.HasLog(RollbackRestartSkippedLog).ShouldBeFalse();
        }

        harness.Seeks.ShouldBeEmpty();
        harness.Resumes.ShouldBeEmpty();
    }
}
