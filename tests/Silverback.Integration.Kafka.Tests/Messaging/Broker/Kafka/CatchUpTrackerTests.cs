// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class CatchUpTrackerTests
{
    private static readonly TopicPartition Partition0 = new("topic", 0);

    private static readonly TopicPartition Partition1 = new("topic", 1);

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly OffsetsTracker _offsetsTracker = new();

    [Theory]
    [InlineData(CatchUpMode.None, false)]
    [InlineData(CatchUpMode.KeepConsuming, true)]
    [InlineData(CatchUpMode.Disconnect, true)]
    public void IsEnabled_ShouldReflectMode(CatchUpMode mode, bool expected)
    {
        CatchUpTracker tracker = new(mode, false, _offsetsTracker);

        tracker.Mode.ShouldBe(mode);
        tracker.IsEnabled.ShouldBe(expected);
    }

    [Fact]
    public void Methods_ShouldReturnNeutralResults_WhenDisabled()
    {
        CatchUpTracker tracker = new(CatchUpMode.None, true, _offsetsTracker);

        tracker.Reset();
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 1));

        tracker.IsCurrent(0).ShouldBeTrue();
        tracker.AddPartitions([Partition0]).ShouldBeNull();
        tracker.RemovePartitions([Partition0], true).ShouldBeNull();
        tracker.OnEndOfPartitionPolled(Partition0).ShouldBe((false, null));
        tracker.CheckPartition(Partition0).ShouldBeNull();
        tracker.IsLastRecordBeforeEnd(Partition0, Guid.NewGuid(), null).ShouldBeFalse();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
        tracker.MoveEndBefore(Partition0, 1).ShouldBeNull();
        tracker.MarkCaughtUp(Partition0).ShouldBeNull();
        tracker.WaitUntilCaughtUpAsync(CancellationToken.None).IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void AddPartitions_ShouldReturnNull_WhenEndOfPartitionsNotReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);

        CatchUpTracker.CaughtUpNotification? notification = tracker.AddPartitions([Partition0]);

        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();
    }

    [Fact]
    public void AddPartitions_ShouldReturnNull_WhenDynamicAssignmentIsEmpty()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);

        CatchUpTracker.CaughtUpNotification? notification = tracker.AddPartitions([]);

        notification.ShouldBeNull();
    }

    [Fact]
    public void AddPartitions_ShouldReturnNotification_WhenStaticAssignmentIsEmpty()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, true, _offsetsTracker);

        CatchUpTracker.CaughtUpNotification? notification = tracker.AddPartitions([]);

        notification.ShouldNotBeNull();
        notification.Generation.ShouldBe(0);
    }

    [Fact]
    public void AddPartitions_ShouldReturnNull_WhenAlreadyCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, true, _offsetsTracker);
        tracker.AddPartitions([]).ShouldNotBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.AddPartitions([Partition0]);

        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
    }

    [Fact]
    public void AddPartitions_ShouldKeepPartitionState_WhenPartitionAddedAgain()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0, Partition1]);
        tracker.OnEndOfPartitionPolled(Partition0).IsEndReached.ShouldBeTrue();

        tracker.AddPartitions([Partition0]).ShouldBeNull();

        tracker.OnEndOfPartitionPolled(Partition0).IsEndReached.ShouldBeFalse();
    }

    [Fact]
    public void RemovePartitions_ShouldReturnNull_WhenNotAssigned()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, true, _offsetsTracker);

        CatchUpTracker.CaughtUpNotification? notification = tracker.RemovePartitions([Partition0], true);

        notification.ShouldBeNull();
    }

    [Fact]
    public void RemovePartitions_ShouldReturnNotification_WhenRemainingPartitionsAreCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0, Partition1]);
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.RemovePartitions([Partition1], true);

        notification.ShouldNotBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
        tracker.IsPartitionCaughtUp(Partition1).ShouldBeTrue();
    }

    [Fact]
    public void RemovePartitions_ShouldReturnNull_WhenCheckCaughtUpIsFalse()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0, Partition1]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));

        CatchUpTracker.CaughtUpNotification? notification = tracker.RemovePartitions([Partition1], false);

        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();
        tracker.CheckPartition(Partition0).ShouldNotBeNull();
    }

    [Fact]
    public void RemovePartitions_ShouldReturnNull_WhenAlreadyCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldNotBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.RemovePartitions([Partition0], true);

        notification.ShouldBeNull();
    }

    [Fact]
    public void RemovePartitions_ShouldReturnNull_WhenAllPartitionsOfDynamicAssignmentRemoved()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        CatchUpTracker.CaughtUpNotification? notification = tracker.RemovePartitions([Partition0], true);

        notification.ShouldBeNull();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNotification_WhenNoRecordPolled()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldNotBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnFalse_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition1);

        isEndReached.ShouldBeFalse();
        notification.ShouldBeNull();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnFalse_WhenEndAlreadyReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 5));
        tracker.OnEndOfPartitionPolled(Partition0).ShouldBe((true, null));

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeFalse();
        notification.ShouldBeNull();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNull_WhenAnotherPartitionIsLagging()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0, Partition1]);

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
        tracker.IsPartitionCaughtUp(Partition1).ShouldBeFalse();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNull_WhenPolledRecordsNotCommitted()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNull_WhenRollbackOffsetIsSpecial()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        _offsetsTracker.TrackPartition(Partition0);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldBeNull();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNotification_WhenPolledRecordsCommitted()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldNotBeNull();
    }

    [Fact]
    public void OnEndOfPartitionPolled_ShouldReturnNotification_WhenSoughtToEnd()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        _offsetsTracker.TrackOffset(new KafkaOffset(Partition0, Offset.End));

        (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = tracker.OnEndOfPartitionPolled(Partition0);

        isEndReached.ShouldBeTrue();
        notification.ShouldNotBeNull();
    }

    [Fact]
    public void OnRecordPolled_ShouldTrackHighestOffset()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 7));
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 5));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();

        _offsetsTracker.Commit(new KafkaOffset(Partition0, 6));
        tracker.CheckPartition(Partition0).ShouldBeNull();

        _offsetsTracker.Commit(new KafkaOffset(Partition0, 7));
        tracker.CheckPartition(Partition0).ShouldNotBeNull();
    }

    [Fact]
    public void OnRecordPolled_ShouldIgnoreRecords_WhenEndAlreadyReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();

        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 10));

        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));
        tracker.CheckPartition(Partition0).ShouldNotBeNull();
    }

    [Fact]
    public void OnRecordPolled_ShouldIgnoreRecords_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        tracker.OnRecordPolled(new TopicPartitionOffset(Partition1, 3));

        tracker.IsPartitionCaughtUp(Partition1).ShouldBeTrue();
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldNotBeNull();
    }

    [Fact]
    public void CheckPartition_ShouldReturnNull_WhenEndNotReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));

        CatchUpTracker.CaughtUpNotification? notification = tracker.CheckPartition(Partition0);

        notification.ShouldBeNull();
    }

    [Fact]
    public void CheckPartition_ShouldReturnNull_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        CatchUpTracker.CaughtUpNotification? notification = tracker.CheckPartition(Partition1);

        notification.ShouldBeNull();
    }

    [Fact]
    public void CheckPartition_ShouldReturnNotification_WhenCommittedAfterEnd()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        _offsetsTracker.TrackOffset(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));

        CatchUpTracker.CaughtUpNotification? notification = tracker.CheckPartition(Partition0);

        notification.ShouldNotBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnFalse_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        bool result = tracker.IsLastRecordBeforeEnd(Partition1, Guid.NewGuid(), null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnFalse_WhenEndNotReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));

        bool result = tracker.IsLastRecordBeforeEnd(Partition0, Guid.NewGuid(), new Offset(3));

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnTrue_WhenRecordIsLastPolledBeforeEnd()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        Guid channelInstanceId = Guid.NewGuid();
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 2));
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0);

        bool resultOffset2 = tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, new Offset(2));
        bool resultOffset3 = tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, new Offset(3));

        resultOffset2.ShouldBeFalse();
        resultOffset3.ShouldBeTrue();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnTrue_WhenEndReadBySameChannelAfterLastRecord()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        Guid channelInstanceId = Guid.NewGuid();
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, new Offset(3)).ShouldBeFalse();
        tracker.OnEndOfPartitionPolled(Partition0);

        bool result = tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnFalse_WhenEndReadByAnotherChannel()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.IsLastRecordBeforeEnd(Partition0, Guid.NewGuid(), new Offset(3));
        tracker.OnEndOfPartitionPolled(Partition0);

        bool result = tracker.IsLastRecordBeforeEnd(Partition0, Guid.NewGuid(), null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnFalse_WhenEndReadBeforeLastRecord()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        Guid channelInstanceId = Guid.NewGuid();
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, new Offset(2));
        tracker.OnEndOfPartitionPolled(Partition0);

        bool result = tracker.IsLastRecordBeforeEnd(Partition0, channelInstanceId, null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsLastRecordBeforeEnd_ShouldReturnFalse_WhenEndReadWithoutAnyRecordRead()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0);

        bool result = tracker.IsLastRecordBeforeEnd(Partition0, Guid.NewGuid(), null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsPartitionCaughtUp_ShouldReturnTrue_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        tracker.IsPartitionCaughtUp(Partition1).ShouldBeTrue();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();
    }

    [Fact]
    public void MoveEndBefore_ShouldReturnNull_WhenEndNotReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 1));

        CatchUpTracker.CaughtUpNotification? notification = tracker.MoveEndBefore(Partition0, 2);

        notification.ShouldBeNull();
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();
    }

    [Fact]
    public void MoveEndBefore_ShouldReturnNull_WhenPartitionNotTracked()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        CatchUpTracker.CaughtUpNotification? notification = tracker.MoveEndBefore(Partition1, 2);

        notification.ShouldBeNull();
    }

    [Fact]
    public void MoveEndBefore_ShouldReturnNotification_WhenRecordsBeforeOffsetCommitted()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 5));
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 2));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.MoveEndBefore(Partition0, 3);

        notification.ShouldNotBeNull();
    }

    [Fact]
    public void MoveEndBefore_ShouldNotMoveEndForward()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 5));
        _offsetsTracker.Commit(new KafkaOffset(Partition0, 2));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();
        tracker.MoveEndBefore(Partition0, 4).ShouldBeNull();

        tracker.MoveEndBefore(Partition0, 10).ShouldBeNull();

        _offsetsTracker.Commit(new KafkaOffset(Partition0, 3));
        tracker.CheckPartition(Partition0).ShouldNotBeNull();
    }

    [Fact]
    public void MarkCaughtUp_ShouldReturnNotification_WhenEndReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.MarkCaughtUp(Partition0);

        notification.ShouldNotBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
    }

    [Fact]
    public void MarkCaughtUp_ShouldReturnNull_WhenEndNotReached()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition0, 3));

        CatchUpTracker.CaughtUpNotification? notification = tracker.MarkCaughtUp(Partition0);

        notification.ShouldBeNull();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();
    }

    [Fact]
    public void MarkCaughtUp_ShouldReturnNull_WhenPartitionAlreadyCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0, Partition1]);
        tracker.OnRecordPolled(new TopicPartitionOffset(Partition1, 3));
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldBeNull();
        tracker.OnEndOfPartitionPolled(Partition1).Notification.ShouldBeNull();

        // Partition 1 is committed until its end, but not checked again: checking the caught up partition again would notify
        _offsetsTracker.Commit(new KafkaOffset(Partition1, 3));

        CatchUpTracker.CaughtUpNotification? notification = tracker.MarkCaughtUp(Partition0);

        notification.ShouldBeNull();
        tracker.CheckPartition(Partition1).ShouldNotBeNull();
    }

    [Fact]
    public void MarkCaughtUp_ShouldReturnNull_WhenConsumerAlreadyCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        tracker.OnEndOfPartitionPolled(Partition0).Notification.ShouldNotBeNull();

        CatchUpTracker.CaughtUpNotification? notification = tracker.MarkCaughtUp(Partition0);

        notification.ShouldBeNull();
    }

    [Fact]
    public void Reset_ShouldIncrementGeneration()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, true, _offsetsTracker);
        tracker.IsCurrent(0).ShouldBeTrue();

        tracker.Reset();

        tracker.IsCurrent(0).ShouldBeFalse();
        tracker.IsCurrent(1).ShouldBeTrue();
        CatchUpTracker.CaughtUpNotification? notification = tracker.AddPartitions([]);
        notification.ShouldNotBeNull();
        notification.Generation.ShouldBe(1);
    }

    [Fact]
    public void Reset_ShouldClearPartitionsAndAssignment()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, true, _offsetsTracker);
        tracker.AddPartitions([Partition0]);

        tracker.Reset();

        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();
        tracker.RemovePartitions([], true).ShouldBeNull();
    }

    [Fact]
    public async Task Reset_ShouldKeepPendingWaiters_WhenNotCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        Task waitTask = tracker.WaitUntilCaughtUpAsync(CancellationToken.None);

        tracker.Reset();
        tracker.AddPartitions([Partition0]);
        CatchUpTracker.CaughtUpNotification? notification = tracker.OnEndOfPartitionPolled(Partition0).Notification;

        notification.ShouldNotBeNull();
        waitTask.IsCompleted.ShouldBeFalse();
        notification.TaskCompletionSource.SetResult();
        await waitTask.WaitAsync(Timeout);
        waitTask.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task Reset_ShouldStartNewCatchUp_WhenCaughtUp()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        tracker.AddPartitions([Partition0]);
        CatchUpTracker.CaughtUpNotification? firstNotification = tracker.OnEndOfPartitionPolled(Partition0).Notification;
        firstNotification.ShouldNotBeNull();
        firstNotification.TaskCompletionSource.SetResult();
        await tracker.WaitUntilCaughtUpAsync(CancellationToken.None).WaitAsync(Timeout);

        tracker.Reset();

        Task waitTask = tracker.WaitUntilCaughtUpAsync(CancellationToken.None);

        waitTask.IsCompleted.ShouldBeFalse();
        tracker.IsPartitionCaughtUp(Partition0).ShouldBeTrue();

        tracker.AddPartitions([Partition0]).ShouldBeNull();

        tracker.IsPartitionCaughtUp(Partition0).ShouldBeFalse();

        CatchUpTracker.CaughtUpNotification? secondNotification = tracker.OnEndOfPartitionPolled(Partition0).Notification;

        secondNotification.ShouldNotBeNull();
        secondNotification.Generation.ShouldBe(1);
        secondNotification.TaskCompletionSource.ShouldNotBeSameAs(firstNotification.TaskCompletionSource);

        secondNotification.TaskCompletionSource.SetResult();
        await waitTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task WaitUntilCaughtUpAsync_ShouldBeCanceled_WhenTokenCanceled()
    {
        CatchUpTracker tracker = new(CatchUpMode.KeepConsuming, false, _offsetsTracker);
        using CancellationTokenSource cancellationTokenSource = new();
        Task waitTask = tracker.WaitUntilCaughtUpAsync(cancellationTokenSource.Token);

        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => waitTask.WaitAsync(Timeout));
    }
}
