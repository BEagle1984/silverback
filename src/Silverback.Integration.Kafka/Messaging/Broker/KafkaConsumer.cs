// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TIdentifier}" />
public class KafkaConsumer : Consumer<KafkaOffset>, IKafkaConsumer, ICumulativeCommitConsumer
{
    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker;

    private readonly ISilverbackLogger<KafkaConsumer> _logger;

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConsumeLoopHandler _consumeLoopHandler;

    private readonly KafkaConsumerEndpointsCache _endpointsCache;

    // Tracks the polled and committed offsets of every partition: the first uncommitted offset is where a restart seeks back to.
    // When processing the partitions together, it also drives the rollback and the cooperative revocation replay.
    private readonly OffsetsTracker _offsets = new();

    private readonly CatchUpTracker _catchUp;

    private readonly System.Threading.Lock _assignmentLock = new(); // serializes partition state transitions and updates to the commit counter

    private readonly HashSet<TopicPartition> _revokedPartitions = [];

    // The partitions whose revocation callback is running, still listed in the assignment until it returns
    private readonly HashSet<TopicPartition> _revokingPartitions = [];

    private readonly Dictionary<TopicPartition, long> _assignmentVersions = [];

    private readonly HashSet<TopicPartition> _rollingBackPartitions = [];

    // The lowest offset requested by the rollbacks of a partition that is already being rolled back (e.g. for a later message, while an
    // earlier batch completed by its max wait time was still being processed and then failed as well), applied when the rollback seeks
    private readonly Dictionary<TopicPartition, (Offset Offset, long Version)> _lowerRollbackOffsets = [];

    // The partitions paused to roll back their records (or to replay the ones retained by a revocation) and not resumed yet: when
    // a stop or a rebalance defeats the rollback restart, they are resumed by the next start or when they are assigned again
    private readonly HashSet<TopicPartition> _rollbackPausedPartitions = [];

    // The partitions paused at their end while catching up to disconnect (see CatchUpMode.Disconnect) and not resumed yet: like the
    // ones paused by a rollback, they are resumed by the next start, unless they are caught up, or when they are assigned again
    private readonly HashSet<TopicPartition> _catchUpPausedPartitions = [];

    // The last offset whose commit failed in each partition, when a client side offset store is used: the offsets are stored before
    // committing, so the rollback must not consume those records again
    private readonly Dictionary<TopicPartition, Offset> _commitFailedOffsets = [];

    // The highest offset committed in each partition since it was assigned. A lower offset isn't committed anymore, since moving the
    // committed offset back would consume the later messages again after a restart, e.g. when an incomplete chunked message is aborted by
    // its timeout after the next messages were committed in a batch.
    private readonly Dictionary<TopicPartition, Offset> _highestCommittedOffsets = [];

    // The partitions sought via Seek since they were assigned, whose highest committed offset isn't tracked anymore: the records polled
    // before the seek (e.g., the one being processed, if sought by its subscriber) are still committed afterwards and, seeking back, their
    // offsets would prevent the records consumed again from being committed.
    private readonly HashSet<TopicPartition> _soughtPartitions = [];

    private Task _channelsStopping = Task.CompletedTask;

    private int _messagesSinceCommit;

    private bool _isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The consumer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IConfluentConsumerWrapper" /> to be used.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="KafkaConsumerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="callbacksInvoker">
    ///     The <see cref="IBrokerClientCallbacksInvoker" />.
    /// </param>
    /// <param name="offsetStoreFactory">
    ///     The <see cref="IKafkaOffsetStoreFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the necessary services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in base class.")]
    public KafkaConsumer(
        string name,
        IConfluentConsumerWrapper client,
        KafkaConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        IServiceProvider serviceProvider,
        ISilverbackLogger<KafkaConsumer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration).Endpoints,
            behaviorsProvider,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client);
        Configuration = Check.NotNull(configuration);

        _offsetStoreFactory = Check.NotNull(offsetStoreFactory);
        _callbacksInvoker = Check.NotNull(callbacksInvoker);
        _logger = Check.NotNull(logger);

        _catchUp = new CatchUpTracker(configuration.CatchUpMode, configuration.IsStaticAssignment, _offsets);
        _channelsManager = new ConsumerChannelsManager(this, callbacksInvoker, logger);
        _consumeLoopHandler = new ConsumeLoopHandler(this, _channelsManager, _offsets, _logger);

        _endpointsCache = new KafkaConsumerEndpointsCache(configuration);

        Client.Consumer = this;
        Client.Initialized.AddHandler(OnClientConnectedAsync);
    }

    /// <inheritdoc cref="IKafkaConsumer.Client" />
    public new IConfluentConsumerWrapper Client { get; }

    /// <inheritdoc cref="IKafkaConsumer.Configuration" />
    public KafkaConsumerConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    public new IReadOnlyCollection<KafkaConsumerEndpointConfiguration> EndpointsConfiguration => Configuration.Endpoints;

    /// <inheritdoc cref="IKafkaConsumer.Pause" />
    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        Check.NotNull(partitions);

        lock (_assignmentLock)
        {
            TopicPartition[] pausedPartitions = [.. partitions];
            Client.Pause(pausedPartitions);

            // The partitions paused by the user must stay paused when a restart resumes the ones paused by a defeated rollback
            _rollbackPausedPartitions.ExceptWith(pausedPartitions);
            _catchUpPausedPartitions.ExceptWith(pausedPartitions);
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.Resume" />
    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        Check.NotNull(partitions);

        lock (_assignmentLock)
        {
            TopicPartition[] resumedPartitions = [.. partitions];
            Client.Resume(resumedPartitions);

            // Not paused anymore: neither a restart nor a new assignment has to resume them
            _rollbackPausedPartitions.ExceptWith(resumedPartitions);
            _catchUpPausedPartitions.ExceptWith(resumedPartitions);
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.Seek" />
    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        Check.NotNull(topicPartitionOffset);

        lock (_assignmentLock)
        {
            Client.Seek(topicPartitionOffset);

            // The new position replaces the tracked one, so that a restart doesn't seek back to the records skipped on purpose.
            // A seek while the partition is being consumed mixes the two positions anyway: the records polled before it may
            // still be tracked, processed and committed afterwards.
            _offsets.UntrackPartition(topicPartitionOffset.TopicPartition);
            _offsets.TrackOffset(topicPartitionOffset);
            _commitFailedOffsets.Remove(topicPartitionOffset.TopicPartition);

            // The records consumed again after seeking back must be committed again
            _highestCommittedOffsets.Remove(topicPartitionOffset.TopicPartition);
            _soughtPartitions.Add(topicPartitionOffset.TopicPartition);

            NotifyIfCaughtUp(_catchUp.CheckPartition(topicPartitionOffset.TopicPartition));
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.GetOffsetsForTimestamps" />
    public IReadOnlyList<TopicPartitionOffset> GetOffsetsForTimestamps(IEnumerable<TopicPartitionTimestamp> topicPartitionTimestamps, TimeSpan timeout) =>
        Client.OffsetsForTimes(topicPartitionTimestamps, timeout);

    /// <inheritdoc cref="IKafkaConsumer.WaitUntilCaughtUpAsync" />
    public Task WaitUntilCaughtUpAsync(CancellationToken cancellationToken = default)
    {
        if (!_catchUp.IsEnabled)
        {
            throw new InvalidOperationException(
                $"The catch-up isn't enabled for this consumer. Set the {nameof(KafkaConsumerConfiguration.CatchUpMode)} " +
                "(e.g. via NotifyWhenCaughtUp or DisconnectWhenCaughtUp).");
        }

        return _catchUp.WaitUntilCaughtUpAsync(cancellationToken);
    }

    internal IReadOnlyCollection<TopicPartitionOffset> OnPartitionsAssigned(IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets)
    {
        // The assignment is never rejected: Confluent.Kafka requires the cooperative one to be returned unchanged, and a rejected
        // eager one would remain unconsumed until the next rebalance. The disconnect revokes it right away.
        if (Client.Status == ClientStatus.Disconnecting)
            return topicPartitionOffsets;

        topicPartitionOffsets = new StoredOffsetsLoader(_offsetStoreFactory, Configuration, ServiceProvider)
            .ApplyStoredOffsets(topicPartitionOffsets);

        lock (_assignmentLock)
        {
            // Accepted without being read: the next start reads the whole assignment
            if (!IsStartedAndNotStopping())
                return topicPartitionOffsets;

            foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionOffsets)
            {
                _rollingBackPartitions.Remove(topicPartitionOffset.TopicPartition);
                _highestCommittedOffsets.Remove(topicPartitionOffset.TopicPartition);
                _soughtPartitions.Remove(topicPartitionOffset.TopicPartition);
                IncrementAssignmentVersion(topicPartitionOffset.TopicPartition);
                _revokedPartitions.Remove(topicPartitionOffset.TopicPartition);
                _revokingPartitions.Remove(topicPartitionOffset.TopicPartition);
                _offsets.TrackOffset(topicPartitionOffset);
                _channelsManager.StartReading(topicPartitionOffset.TopicPartition);
            }

            NotifyIfCaughtUp(_catchUp.AddPartitions(topicPartitionOffsets.Select(offset => offset.TopicPartition)));

            // The native pause survives the revocation and the new assignment of a partition (with both the eager and the
            // cooperative assignors): resume the partitions paused by a rollback that the rebalance defeated (or at their end while
            // catching up), or they would never be fetched again. Resumed before the assignment is applied, they are fetched from the
            // assigned offsets only. A native consumer that is still initializing has no paused partitions. Once the consumer is caught
            // up, the partitions paused at their end stay paused until it's disconnected.
            if ((_rollbackPausedPartitions.Count > 0 || _catchUpPausedPartitions.Count > 0) && Client.Status == ClientStatus.Initialized)
            {
                HashSet<TopicPartition> assignedPartitions = [.. topicPartitionOffsets.Select(offset => offset.TopicPartition)];
                ResumeRollbackPausedPartitionsCore(assignedPartitions.Contains);
                ResumeCatchUpPausedPartitionsCore(partition =>
                    assignedPartitions.Contains(partition) && !_catchUp.IsPartitionCaughtUp(partition));
            }
        }

        SetConnectedStatus();

        return topicPartitionOffsets;
    }

    internal void OnPartitionsRevoked(IReadOnlyList<TopicPartitionOffset> topicPartitionOffsets)
    {
        PartitionChannel[] channels;
        List<RollbackPartition> retained = [];

        lock (_assignmentLock)
        {
            HashSet<TopicPartition> revoked = [.. topicPartitionOffsets.Select(offset => offset.TopicPartition)];
            channels =
            [
                .. topicPartitionOffsets.Select(offset => _channelsManager.GetChannel(offset.TopicPartition))
                    .OfType<PartitionChannel>().Distinct()
            ];

            foreach (TopicPartition partition in revoked)
            {
                _revokedPartitions.Add(partition);
                _revokingPartitions.Add(partition);
                _rollingBackPartitions.Remove(partition);
                _commitFailedOffsets.Remove(partition);
                _highestCommittedOffsets.Remove(partition);
                _soughtPartitions.Remove(partition);
                IncrementAssignmentVersion(partition);
            }

            // The revocation of a stopping consumer (e.g. disconnecting) doesn't make it caught up
            bool canCatchUp = IsStartedAndNotStopping() && Client.Status != ClientStatus.Disconnecting;
            NotifyIfCaughtUp(_catchUp.RemovePartitions(revoked, canCatchUp));

            // A shared channel also contains records from retained partitions. Rebuild it once and replay those records.
            if (!Configuration.ProcessPartitionsIndependently && topicPartitionOffsets.Count > 0)
            {
                foreach (KafkaOffset offset in _offsets.GetRollbackOffSets().Where(offset => !revoked.Contains(offset.TopicPartition) &&
                                                                                             IsNotRevoked(offset.TopicPartition)))
                {
                    long version = IncrementAssignmentVersion(offset.TopicPartition);
                    _rollingBackPartitions.Add(offset.TopicPartition);
                    retained.Add(new RollbackPartition(offset.AsTopicPartitionOffset(), version));
                }

                if (retained.Count > 0 && IsStartedAndNotStopping())
                {
                    TopicPartition[] pausedPartitions = [.. retained.Select(partition => partition.Offset.TopicPartition)];
                    Client.Pause(pausedPartitions);
                    _rollbackPausedPartitions.UnionWith(pausedPartitions);
                }
            }
        }

        RevertConnectedStatus();

        // Callbacks execute inside Consume, so polling cannot enqueue records until this callback returns.
        // Never hold the assignment lock while draining a handler or aborting a sequence.
        Task.WhenAll(channels.Select(_channelsManager.StopChannelAsync)).SafeWait();

        lock (_assignmentLock)
        {
            if (!Configuration.EnableAutoCommit)
                Client.Commit();

            foreach (TopicPartition topicPartition in topicPartitionOffsets.Select(offset => offset.TopicPartition))
            {
                _offsets.UntrackPartition(topicPartition);
                _revokingPartitions.Remove(topicPartition);
            }

            foreach (TopicPartitionOffset rollbackOffset in retained.Where(IsCurrentRollback).Select(partition => partition.Offset))
            {
                if (rollbackOffset.Offset != Offset.Unset)
                    Client.Seek(rollbackOffset);

                _channelsManager.StartReading(rollbackOffset.TopicPartition);
                ResumeReplayedPartitionCore(rollbackOffset.TopicPartition);
                _rollingBackPartitions.Remove(rollbackOffset.TopicPartition);
            }
        }
    }

    /// <summary>
    ///     Starts reading the assigned partitions again, including the ones the start missed.
    /// </summary>
    /// <remarks>
    ///     Called by the consume loop before its first poll, once the previous loop exited, and before
    ///     <see cref="SeekToUncommittedOffsets" />. An assignment accepted by the last poll of a stopping loop is applied by
    ///     Confluent.Kafka only after the callback returns, possibly after the start read the assignment. The partitions already
    ///     being read are unaffected, since no record was polled since the start.
    /// </remarks>
    internal void StartReadingAssignment()
    {
        lock (_assignmentLock)
        {
            if (!IsStartedAndNotStopping())
                return;

            StartReadingAssignmentCore();
        }
    }

    /// <summary>
    ///     Seeks every assigned partition back to its first uncommitted offset, so that the records discarded by a stop
    ///     (buffered, being processed or belonging to an aborted sequence) are consumed again.
    /// </summary>
    /// <remarks>
    ///     Called by the consume loop before its first poll. The offsets are determined when starting and not when stopping,
    ///     because the stopping loop may still return one more record and the draining subscribers may still commit.
    /// </remarks>
    internal void SeekToUncommittedOffsets()
    {
        lock (_assignmentLock)
        {
            if (!IsStartedAndNotStopping())
                return;

            IReadOnlyCollection<KafkaOffset> replayOffsets = _offsets.GetReplayOffsets();

            if (replayOffsets.Count == 0)
                return;

            HashSet<TopicPartition> assignment = [.. Client.Assignment];

            foreach (KafkaOffset offset in replayOffsets)
            {
                // A partition being rolled back is sought by the rollback itself
                if (!assignment.Contains(offset.TopicPartition) || !IsNotRevoked(offset.TopicPartition) ||
                    _rollingBackPartitions.Contains(offset.TopicPartition))
                {
                    continue;
                }

                TopicPartitionOffset topicPartitionOffset = offset.AsTopicPartitionOffset();
                Client.Seek(topicPartitionOffset);
                _logger.LogPartitionOffsetReset(topicPartitionOffset, this);
            }
        }
    }

    /// <summary>
    ///     Resumes the assigned partitions that were paused by a rollback whose restart was defeated by a stop, or at their end while
    ///     catching up and not caught up yet: they would otherwise never be fetched again.
    /// </summary>
    /// <remarks>
    ///     Called by the consume loop before its first poll, after <see cref="SeekToUncommittedOffsets" />, so that nothing is
    ///     fetched from the position preceding the seek. The partitions paused by the user after the rollback paused them are not
    ///     resumed.
    /// </remarks>
    internal void ResumeRollbackPausedPartitions()
    {
        lock (_assignmentLock)
        {
            if (_rollbackPausedPartitions.Count == 0 && _catchUpPausedPartitions.Count == 0 ||
                !IsStartedAndNotStopping() ||
                Client.Status != ClientStatus.Initialized)
            {
                return;
            }

            HashSet<TopicPartition> assignment = [.. Client.Assignment];

            // A partition being rolled back again is resumed by the rollback itself
            bool IsResumable(TopicPartition partition) =>
                assignment.Contains(partition) && IsNotRevoked(partition) && !_rollingBackPartitions.Contains(partition);

            ResumeRollbackPausedPartitionsCore(IsResumable);

            // A caught up partition doesn't need to be consumed any further
            ResumeCatchUpPausedPartitionsCore(partition => IsResumable(partition) && !_catchUp.IsPartitionCaughtUp(partition));
        }
    }

    /// <summary>
    ///     Returns the specified offsets that can be stored to the client side offset store right now: the ones that the commit would
    ///     commit (not lower than the offset already committed, of a partition that is still owned, i.e. neither revoked nor being rolled
    ///     back) and, if <paramref name="includeRevokedPartitions" /> is <c>true</c>, the ones of the revoked partitions.
    /// </summary>
    /// <remarks>
    ///     Used by the client side offset store, to store the same offsets that the consumer commits. The only exception are the offsets
    ///     of the messages processed while their partition is being revoked (see <see cref="KafkaOffsetStoreScope.StoreOffsetsAsync" />):
    ///     the consumer doesn't commit them, but they're stored atomically with the changes done by the subscribers, so that the next
    ///     owner of the partition doesn't process them again. The offsets of a partition being rolled back are never stored, like they
    ///     aren't committed: the rollback seeks back to the lowest rolled back offset, which may precede them, and the stored offset must
    ///     not skip the rolled back messages if they aren't processed again (e.g., because the consumer is stopped).
    /// </remarks>
    /// <param name="offsets">
    ///     The offsets to be stored.
    /// </param>
    /// <param name="includeRevokedPartitions">
    ///     Whether the offsets of the revoked partitions can be stored as well.
    /// </param>
    /// <returns>
    ///     The offsets that can be stored.
    /// </returns>
    internal IReadOnlyCollection<KafkaOffset> GetStorableOffsets(IEnumerable<KafkaOffset> offsets, bool includeRevokedPartitions)
    {
        lock (_assignmentLock)
        {
            return
            [
                .. offsets.Where(offset => IsCommittableOffsetCore(offset) ||
                                           includeRevokedPartitions && !IsNotRevoked(offset.TopicPartition))
            ];
        }
    }

    internal bool OnPollTimeout(LogMessage logMessage)
    {
        if (Configuration.EnableAutoRecovery)
        {
            _logger.LogPollTimeoutAutoRecovery(logMessage, this);
            TriggerReconnectAsync().FireAndForget();
        }
        else
        {
            _logger.LogPollTimeoutNoAutoRecovery(logMessage, this);
            RevertConnectedStatus();
        }

        return true;
    }

    // Called by the consume loop for each polled record, while catching up
    internal void OnRecordPolled(TopicPartitionOffset topicPartitionOffset) => _catchUp.OnRecordPolled(topicPartitionOffset);

    // Called by the consume loop when the end of a partition is polled, while catching up
    internal void OnEndOfPartitionPolled(TopicPartition topicPartition)
    {
        if (!_catchUp.IsEnabled)
            return;

        lock (_assignmentLock)
        {
            (bool isEndReached, CatchUpTracker.CaughtUpNotification? notification) = _catchUp.OnEndOfPartitionPolled(topicPartition);

            if (isEndReached)
                PauseAtEndCore(topicPartition);

            NotifyIfCaughtUp(notification);
        }
    }

    // Called by the channel reader after a record (or the end of the partition, with a null offset) is read, while catching up. The
    // last record before the end completes the pending batches, which otherwise wouldn't complete until full or until their max wait
    // time elapses. The end of the partition isn't reported again when the records are consumed again (e.g. after a rollback or a
    // restart), so the last record is checked as well, also to pause the partition again.
    // All records before the end are dispatched by then: the ones that aren't committed yet are either held by the sequences in the
    // store (being processed, or pending because they don't complete at the end) or never committed at all (e.g. filtered out or
    // unhandled in a batch, which commits them only with the next batch). The end is therefore moved to the last record that will be
    // committed, but before any sequence that doesn't complete: the partition is caught up once a stream is read, or once the records
    // before an incomplete chunked message (and before the batch containing it, which waits for its missing chunks) are committed.
    // The reader of a channel replaced by a restart (or reset by a rollback) may still be running: it's ignored, since the records are
    // consumed again by the current channel.
    internal async ValueTask OnRecordReadAsync(PartitionChannel channel, TopicPartition topicPartition, Offset? offset)
    {
        Guid channelInstanceId = channel.InstanceId;
        ISequenceStore sequenceStore = channel.SequenceStore;

        if (!_catchUp.IsLastRecordBeforeEnd(topicPartition, channelInstanceId, offset))
            return;

        if (offset != null)
        {
            lock (_assignmentLock)
            {
                if (IsCurrentChannelCore(channel, channelInstanceId, topicPartition))
                    PauseAtEndCore(topicPartition);
            }
        }

        foreach (BatchSequence batchSequence in sequenceStore.GetPendingSequences().OfType<BatchSequence>())
        {
            await batchSequence.CompleteEarlyAsync().ConfigureAwait(false);
        }

        // A shared channel streams the records of all its endpoints, since they must all use the same batch settings
        IReadOnlyCollection<ISequence> pendingSequences = sequenceStore.GetPendingSequences(includeUnbounded: true, includeChildren: true);
        bool isStreaming = pendingSequences.Any(sequence => sequence is UnboundedSequence);

        // A batch still pending is being aborted (e.g. a subscriber failed before it could be completed): the outcome is tracked via
        // its commit or rollback
        if (!isStreaming && pendingSequences.Any(sequence => sequence is BatchSequence && sequence.ParentSequence == null))
            return;

        long? lastOffset = GetLastCommittableOffset(topicPartition, sequenceStore, pendingSequences);

        lock (_assignmentLock)
        {
            // A rollback (registered before its sequence leaves the store) or a revocation replays the records: the uncommitted ones
            // aren't the ones that will never be committed
            if (!IsStartedAndNotStopping() || !IsNotRevoked(topicPartition) || _rollingBackPartitions.Contains(topicPartition) ||
                !IsCurrentChannelCore(channel, channelInstanceId, topicPartition))
            {
                return;
            }

            if (isStreaming)
                NotifyIfCaughtUp(_catchUp.MarkCaughtUp(topicPartition));
            else if (lastOffset != null)
                NotifyIfCaughtUp(_catchUp.MoveEndBefore(topicPartition, lastOffset.Value + 1));
        }
    }

    internal async Task HandleMessageAsync(
        Message<byte[]?, byte[]?> message,
        TopicPartitionOffset topicPartitionOffset,
        ISequenceStore sequenceStore,
        Guid sourceChannelInstanceId)
    {
        // Reserve room for the key and timestamp headers
        MessageHeaderCollection headers = message.Headers.ToSilverbackHeaders(2);

        KafkaConsumerEndpoint endpoint = _endpointsCache.GetEndpoint(topicPartitionOffset.TopicPartition);

        if (message.Key != null)
            headers.AddOrReplace(KafkaMessageHeaders.MessageKey, Encoding.UTF8.GetString(message.Key));

        headers.AddOrReplace(KafkaMessageHeaders.Timestamp, message.Timestamp.UtcDateTime.ToString("O"));

        await HandleMessageAsync(
                message.Value,
                headers,
                endpoint,
                new KafkaOffset(topicPartitionOffset, sourceChannelInstanceId),
                sequenceStore)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StartCoreAsync" />
    protected override ValueTask StartCoreAsync()
    {
        lock (_assignmentLock)
        {
            StartReadingAssignmentCore();
        }

        // Assignment callbacks are delivered by Consume, so polling must start before assignment
        StartConsumeLoopHandler();

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StopCoreAsync()" />
    protected override ValueTask StopCoreAsync()
    {
        lock (_assignmentLock)
        {
            foreach (TopicPartition partition in _assignmentVersions.Keys)
            {
                _assignmentVersions[partition]++;
            }
        }

        _consumeLoopHandler.StopAsync().FireAndForget();
        _channelsStopping = _channelsManager.StopReadingAsync();

        return default;
    }

    /// <inheritdoc />
    protected override bool TryBeginStop(KafkaOffset? brokerMessageIdentifier)
    {
        lock (_assignmentLock)
        {
            if (brokerMessageIdentifier != null && !IsCurrentOffset(brokerMessageIdentifier))
                return false;

            return base.TryBeginStop(brokerMessageIdentifier);
        }
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override async ValueTask WaitUntilConsumingStoppedCoreAsync() =>
        await Task.WhenAll(WaitUntilChannelsManagerStopsAsync(), WaitUntilConsumeLoopHandlerStopsAsync()).ConfigureAwait(false);

    /// <inheritdoc cref="Consumer{TIdentifier}.CommitCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override ValueTask CommitCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers);

        lock (_assignmentLock)
        {
            // A non-blocking stop marks the consumer stopped before its completed batches finish committing.
            // Channel ownership remains valid until draining removes the channel.
            KafkaOffset[] offsets = [.. brokerMessageIdentifiers.Where(IsCommittableOffsetCore)];
            bool stored = false;

            try
            {
                foreach (KafkaOffset offset in offsets)
                {
                    // Tracked as committed only once stored: a record whose offset couldn't be stored must still be consumed again
                    // after a restart (or a shared rollback)
                    StoreOffset(new TopicPartitionOffset(offset.TopicPartition, offset.Offset + 1));
                    _offsets.Commit(offset);

                    if (!_soughtPartitions.Contains(offset.TopicPartition))
                        _highestCommittedOffsets[offset.TopicPartition] = offset.Offset;

                    stored = true;

                    NotifyIfCaughtUp(_catchUp.CheckPartition(offset.TopicPartition));
                }

                if (stored)
                    CommitOffsetsIfNeeded();
            }
            catch (Exception) when (Configuration.ClientSideOffsetStore != null)
            {
                foreach (KafkaOffset offset in offsets)
                {
                    _commitFailedOffsets[offset.TopicPartition] = offset.Offset;
                }

                throw;
            }
        }

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.RollbackCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override ValueTask RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers);

        Dictionary<TopicPartition, long> versions;

        lock (_assignmentLock)
        {
            if (!IsStartedAndNotStopping())
                return ValueTask.CompletedTask;

            // Capture before enumerating offsets: enumeration itself may overlap a rebalance
            versions = new Dictionary<TopicPartition, long>(_assignmentVersions);
        }

        KafkaOffset[] requestedOffsets = [.. brokerMessageIdentifiers];
        List<RollbackPartition> partitions = [];
        HashSet<PartitionChannel> channels = [];
        Task pollCompleted;

        lock (_assignmentLock)
        {
            // The shared channel rolls back to the lowest uncommitted offsets anyway (see below)
            if (Configuration.ProcessPartitionsIndependently)
                TrackLowerRollbackOffsetsCore(requestedOffsets, versions);

            KafkaOffset[] currentOffsets =
            [
                .. requestedOffsets.Where(offset =>
                    IsCurrentOffset(offset) && versions.TryGetValue(offset.TopicPartition, out long version) &&
                    _assignmentVersions.GetValueOrDefault(offset.TopicPartition) == version)
            ];

            // An old shared sequence must never expand its rollback to a replacement channel's offsets
            if (currentOffsets.Length == 0 ||
                !Configuration.ProcessPartitionsIndependently && currentOffsets.Length != requestedOffsets.Length)
            {
                _logger.LogRollbackRestartSkipped(this);

                return ValueTask.CompletedTask;
            }

            // The rollback can include several offsets per partition (e.g. the failed batch and the batch started in the meantime):
            // seek to the lowest one, to consume all of them again
            IEnumerable<KafkaOffset> offsets = Configuration.ProcessPartitionsIndependently
                ? GetLowestOffsetPerPartition(currentOffsets)
                : _offsets.GetRollbackOffSets();

            foreach (KafkaOffset offset in offsets)
            {
                // Shared rollback offsets may have been tracked by a previous channel in the same assignment.
                // The requesting transaction was validated above; validate these positions by assignment instead.
                if (!IsStartedAndNotStopping() || !IsNotRevoked(offset.TopicPartition) ||
                    _rollingBackPartitions.Contains(offset.TopicPartition) ||
                    !versions.TryGetValue(offset.TopicPartition, out long version) ||
                    _assignmentVersions.GetValueOrDefault(offset.TopicPartition) != version ||
                    _channelsManager.GetChannel(offset.TopicPartition) is not { } channel)
                {
                    continue;
                }

                _rollingBackPartitions.Add(offset.TopicPartition);
                channels.Add(channel);
                partitions.Add(new RollbackPartition(offset.AsTopicPartitionOffset(), version));
            }

            if (partitions.Count == 0)
                return ValueTask.CompletedTask;

            TopicPartition[] pausedPartitions = [.. partitions.Select(partition => partition.Offset.TopicPartition)];
            Client.Pause(pausedPartitions);
            _rollbackPausedPartitions.UnionWith(pausedPartitions);
            pollCompleted = _consumeLoopHandler.WaitUntilCurrentPollCompletesAsync();
        }

        // Stop the captured instances, never whichever channel now happens to occupy their partition keys
        Task stopping = Task.WhenAll(channels.Select(_channelsManager.StopChannelAsync).Append(pollCompleted));

        Task.Run(() => RestartChannelsAfterRollbackAsync(stopping, partitions)).FireAndForget();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _isDisposed)
            return;

        _consumeLoopHandler.Dispose();
        _channelsManager.Dispose();

        Client.Initialized.RemoveHandler(OnClientConnectedAsync);

        _isDisposed = true;
    }

    private static IEnumerable<long?> GetPartitionOffsets(
        IEnumerable<ISequence> sequences,
        TopicPartition topicPartition,
        Func<ISequence, IEnumerable<IBrokerMessageIdentifier>> getIdentifiers) =>
        sequences
            .SelectMany(getIdentifiers)
            .OfType<KafkaOffset>()
            .Where(offset => offset.TopicPartition == topicPartition && !offset.IsSpecial)
            .Select(offset => (long?)offset.Offset.Value);

    // The special offsets (e.g. unset) compare lower than any actual offset: prefer the actual ones
    private static IEnumerable<KafkaOffset> GetLowestOffsetPerPartition(IEnumerable<KafkaOffset> offsets) =>
        offsets
            .GroupBy(offset => offset.TopicPartition)
            .Select(partitionOffsets => partitionOffsets.Aggregate((lowest, offset) =>
                !offset.IsSpecial && (lowest.IsSpecial || offset < lowest) ? offset : lowest));

    // Returns the last offset of the partition that will be committed: the highest one held by the sequences in the store (being
    // processed) or already committed, but before the first sequence that doesn't complete (an incomplete chunked message, or the
    // batch containing it, which waits for its missing chunks). The offsets held from there on don't count, or a record before the
    // incomplete sequence that is never committed (e.g. filtered out in a batch) would be waited for.
    private long? GetLastCommittableOffset(
        TopicPartition topicPartition,
        ISequenceStore sequenceStore,
        IReadOnlyCollection<ISequence> pendingSequences)
    {
        long? firstIncompleteOffset = GetPartitionOffsets(
                pendingSequences,
                topicPartition,
                sequence => (sequence.ParentSequence ?? sequence).GetRollbackIdentifiers())
            .Min();
        long? lastInFlightOffset = GetPartitionOffsets(sequenceStore, topicPartition, sequence => sequence.GetCommitIdentifiers())
            .Where(offset => firstIncompleteOffset == null || offset < firstIncompleteOffset)
            .Max();

        // Read after the sequences in the store: a sequence committed and removed from the store meanwhile is reflected here
        KafkaOffset? rollbackOffset = _offsets.GetRollbackOffset(topicPartition);
        long? lastCommittedOffset = rollbackOffset is { IsSpecial: false } ? rollbackOffset.Offset.Value - 1 : null;

        long? lastOffset = new[] { lastInFlightOffset, lastCommittedOffset }.Max();

        if (firstIncompleteOffset == null)
            return lastOffset;

        return lastOffset == null ? firstIncompleteOffset - 1 : Math.Min(lastOffset.Value, firstIncompleteOffset.Value - 1);
    }

    private ValueTask OnClientConnectedAsync(BrokerClient client)
    {
        // The native consumer was just (re)created: nothing tracked before can be consumed again from it, the new consumer resumes
        // from the committed offsets and none of its partitions is paused
        lock (_assignmentLock)
        {
            _offsets.Clear();
            _rollbackPausedPartitions.Clear();
            _catchUpPausedPartitions.Clear();
            _revokingPartitions.Clear();
            _commitFailedOffsets.Clear();
            _highestCommittedOffsets.Clear();
            _soughtPartitions.Clear();
            _catchUp.Reset();
        }

        return StartAsync();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    [SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Concurrency-sensitive channels restart, where splitting would obscure the stop and restart ordering")]
    private async Task RestartChannelsAfterRollbackAsync(Task stopping, IReadOnlyCollection<RollbackPartition> partitions)
    {
        try
        {
            await stopping.ConfigureAwait(false);

            StoredOffsetsLoader storedOffsetsLoader = new(_offsetStoreFactory, Configuration, ServiceProvider);
            bool hasFailedCommit;

            lock (_assignmentLock)
            {
                hasFailedCommit = partitions.Any(partition => _commitFailedOffsets.ContainsKey(partition.Offset.TopicPartition));
            }

            // Loaded before taking the lock again, since the store is usually a database
            if (hasFailedCommit)
                storedOffsetsLoader.LoadStoredOffsets();

            lock (_assignmentLock)
            {
                RollbackPartition[] current = [.. partitions.Where(IsCurrentRollback)];

                if (current.Length != partitions.Count)
                {
                    _logger.LogRollbackRestartSkipped(this);

                    if (!Configuration.ProcessPartitionsIndependently)
                        return;
                }

                foreach (RollbackPartition partition in current)
                {
                    TopicPartitionOffset rollbackOffset = partition.Offset;
                    TopicPartitionOffset offset = rollbackOffset;

                    // A rollback requested meanwhile may need a lower offset (see TrackLowerRollbackOffsetsCore)
                    if (_lowerRollbackOffsets.Remove(rollbackOffset.TopicPartition, out (Offset Offset, long Version) lowerOffset) &&
                        lowerOffset.Version == partition.Version &&
                        (offset.Offset == Offset.Unset || lowerOffset.Offset.Value < offset.Offset.Value))
                    {
                        offset = new TopicPartitionOffset(offset.TopicPartition, lowerOffset.Offset);
                    }

                    // The pending poll may have discovered this shared partition's first record after rollback began
                    if (offset.Offset == Offset.Unset &&
                        _offsets.GetRollbackOffSets().FirstOrDefault(candidate => candidate.TopicPartition == offset.TopicPartition) is { } trackedOffset)
                    {
                        offset = trackedOffset.AsTopicPartitionOffset();
                    }

                    // A record fetched before Pause must finish dispatching before Seek and channel replacement
                    if (offset.Offset != Offset.Unset)
                    {
                        offset = SkipStoredOffsetsCore(offset, storedOffsetsLoader);
                        Client.Seek(offset);
                        _logger.LogPartitionOffsetReset(offset, this);
                    }

                    // The old channel was removed by StopChannelAsync; no sequence disposal is performed under this lock
                    _channelsManager.StartReading(rollbackOffset.TopicPartition);
                    ResumeReplayedPartitionCore(rollbackOffset.TopicPartition);
                    _rollingBackPartitions.Remove(rollbackOffset.TopicPartition);
                }
            }
        }
        catch (Exception ex)
        {
            lock (_assignmentLock)
            {
                if (!partitions.Any(IsCurrentRollback))
                    return;
            }

            _logger.LogConsumerStartError(this, ex);
            await TriggerReconnectAsync().ConfigureAwait(false);
        }
    }

    // Must be called while holding the assignment lock. After a failed commit the records in the client side offset store are already
    // processed: like when reassigning the partition, they must not be consumed again. This doesn't apply to the other rollbacks, where
    // an offset may be stored by a subscriber while the processing failed afterwards, and the message must be retried: the rollback of
    // a later record is one of those, even if the failed commit wasn't rolled back (e.g. an error policy committed it again).
    private TopicPartitionOffset SkipStoredOffsetsCore(TopicPartitionOffset offset, StoredOffsetsLoader storedOffsetsLoader)
    {
        if (!_commitFailedOffsets.Remove(offset.TopicPartition, out Offset failedOffset) || failedOffset < offset.Offset)
            return offset;

        TopicPartitionOffset nextOffset = storedOffsetsLoader.ApplyStoredOffset(offset);

        if (nextOffset.Offset != offset.Offset)
        {
            _offsets.UntrackPartition(offset.TopicPartition);
            _offsets.TrackOffset(nextOffset);
            NotifyIfCaughtUp(_catchUp.CheckPartition(offset.TopicPartition));
        }

        return nextOffset;
    }

    // Must be called while holding the assignment lock
    private void ResumeRollbackPausedPartitionsCore(Func<TopicPartition, bool> predicate)
    {
        TopicPartition[] partitions = [.. _rollbackPausedPartitions.Where(predicate)];

        if (partitions.Length == 0)
            return;

        _rollbackPausedPartitions.ExceptWith(partitions);

        // A partition paused at its end while catching up that is caught up stays paused
        partitions = [.. partitions.Where(partition => !IsKeptPausedAtEndCore(partition))];

        if (partitions.Length == 0)
            return;

        Client.Resume(partitions);
        _catchUpPausedPartitions.ExceptWith(partitions);

        foreach (TopicPartition partition in partitions)
        {
            _logger.LogPartitionResumed(partition, this);
        }
    }

    // Must be called while holding the assignment lock
    private void ResumeCatchUpPausedPartitionsCore(Func<TopicPartition, bool> predicate)
    {
        TopicPartition[] partitions = [.. _catchUpPausedPartitions.Where(predicate)];

        if (partitions.Length == 0)
            return;

        Client.Resume(partitions);
        _catchUpPausedPartitions.ExceptWith(partitions);

        foreach (TopicPartition partition in partitions)
        {
            _logger.LogPartitionResumed(partition, this);
        }
    }

    // Must be called while holding the assignment lock. Resumes the partition paused to replay its records (by a rollback or by the
    // revocation of the other partitions sharing its channel), unless it's paused at its end while catching up and it's caught up:
    // its end isn't reported again, so it would never be paused again.
    private void ResumeReplayedPartitionCore(TopicPartition topicPartition)
    {
        _rollbackPausedPartitions.Remove(topicPartition);

        if (IsKeptPausedAtEndCore(topicPartition))
            return;

        Client.Resume([topicPartition]);
        _catchUpPausedPartitions.Remove(topicPartition);
        _logger.LogPartitionResumed(topicPartition, this);
    }

    // Must be called while holding the assignment lock
    private bool IsKeptPausedAtEndCore(TopicPartition topicPartition) =>
        _catchUpPausedPartitions.Contains(topicPartition) && _catchUp.IsPartitionCaughtUp(topicPartition);

    // Must be called while holding the assignment lock. The partition doesn't need to be consumed any further before disconnecting.
    // Must be called while holding the assignment lock
    private bool IsCurrentChannelCore(PartitionChannel channel, Guid channelInstanceId, TopicPartition topicPartition) =>
        !channel.ReadCancellationToken.IsCancellationRequested && channel.InstanceId == channelInstanceId &&
        _channelsManager.GetChannel(topicPartition) == channel;

    private void PauseAtEndCore(TopicPartition topicPartition)
    {
        if (_catchUp.Mode != CatchUpMode.Disconnect || _catchUpPausedPartitions.Contains(topicPartition) ||
            !IsNotRevoked(topicPartition) || Client.Status != ClientStatus.Initialized)
        {
            return;
        }

        Client.Pause([topicPartition]);
        _catchUpPausedPartitions.Add(topicPartition);
    }

    // The notification runs in its own task: it's triggered by the polling, the reading or the commit, which the disconnect waits for
    private void NotifyIfCaughtUp(CatchUpTracker.CaughtUpNotification? notification)
    {
        if (notification != null)
            Task.Run(() => OnCaughtUpAsync(notification)).FireAndForget();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task OnCaughtUpAsync(CatchUpTracker.CaughtUpNotification notification)
    {
        _logger.LogConsumerCaughtUp(this);

        // Completed before invoking the callbacks, which may await it
        notification.TaskCompletionSource.TrySetResult();

        try
        {
            await _callbacksInvoker.InvokeAsync<IKafkaConsumerCaughtUpCallback>(callback => callback.OnConsumerCaughtUpAsync(this))
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Already logged by the invoker: the consumer is caught up anyway
        }

        // A reconnect started a new catch-up meanwhile
        if (_catchUp.Mode != CatchUpMode.Disconnect || !_catchUp.IsCurrent(notification.Generation))
            return;

        try
        {
            await Client.DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCaughtUpConsumerDisconnectError(this, ex);
        }
    }

    private bool IsCurrentRollback(RollbackPartition partition) =>
        IsStartedAndNotStopping() && IsNotRevoked(partition.Offset.TopicPartition) &&
        _rollingBackPartitions.Contains(partition.Offset.TopicPartition) &&
        _assignmentVersions.GetValueOrDefault(partition.Offset.TopicPartition) == partition.Version;

    private bool IsCurrentOffset(KafkaOffset offset) =>
        IsStartedAndNotStopping() && IsOwnedOffset(offset);

    // Must be called while holding the assignment lock. The rollback of a partition that is already being rolled back is otherwise
    // skipped: its offsets are tracked instead, so that the pending rollback seeks to the lowest one.
    private void TrackLowerRollbackOffsetsCore(IEnumerable<KafkaOffset> offsets, Dictionary<TopicPartition, long> versions)
    {
        if (!IsStartedAndNotStopping())
            return;

        foreach (KafkaOffset offset in offsets)
        {
            if (offset.IsSpecial || !_rollingBackPartitions.Contains(offset.TopicPartition) || !IsNotRevoked(offset.TopicPartition) ||
                !versions.TryGetValue(offset.TopicPartition, out long version) ||
                _assignmentVersions.GetValueOrDefault(offset.TopicPartition) != version ||
                _channelsManager.GetChannel(offset.TopicPartition) is not { } channel ||
                offset.HasSourceChannel && !offset.BelongsToChannel(channel.InstanceId))
            {
                continue;
            }

            if (_lowerRollbackOffsets.TryGetValue(offset.TopicPartition, out (Offset Offset, long Version) lowerOffset) &&
                lowerOffset.Version == version && lowerOffset.Offset.Value <= offset.Offset.Value)
            {
                continue;
            }

            _lowerRollbackOffsets[offset.TopicPartition] = (offset.Offset, version);
        }
    }

    private bool IsOwnedOffset(KafkaOffset offset) =>
        IsNotRevoked(offset.TopicPartition) && !_rollingBackPartitions.Contains(offset.TopicPartition) &&
        _channelsManager.GetChannel(offset.TopicPartition) is { } channel &&
        (!offset.HasSourceChannel || offset.BelongsToChannel(channel.InstanceId));

    // Must be called while holding the assignment lock. The offset already committed can be committed again (e.g., by the error policy
    // skipping the message whose commit failed).
    private bool IsCommittableOffsetCore(KafkaOffset offset) =>
        IsOwnedOffset(offset) &&
        (!_highestCommittedOffsets.TryGetValue(offset.TopicPartition, out Offset committedOffset) || offset.Offset >= committedOffset);

    private void StartConsumeLoopHandler()
    {
        if (!(IsStarted || IsStarting) || IsStopping)
            return;

        _consumeLoopHandler.Start();

        _logger.LogConsumeLoopHandlerStarted(this, _consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id);
    }

    private async Task WaitUntilConsumeLoopHandlerStopsAsync()
    {
        _logger.LogConsumeLoopHandlerStopAwaiting(this, _consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id);

        await _consumeLoopHandler.Stopping.ConfigureAwait(false);

        _logger.LogConsumeLoopHandlerStopped(this, _consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id);
    }

    private async Task WaitUntilChannelsManagerStopsAsync()
    {
        _logger.LogChannelsManagerStopAwaiting(this);

        await _channelsStopping.ConfigureAwait(false);

        _logger.LogChannelsManagerStopped(this);
    }

    private void StoreOffset(TopicPartitionOffset offset)
    {
        _logger.LogOffsetStoring(this, offset);

        Client.StoreOffset(offset);
    }

    private void CommitOffsetsIfNeeded()
    {
        if (Configuration.EnableAutoCommit)
            return;

        if (++_messagesSinceCommit < Configuration.CommitOffsetEach)
            return;

        _messagesSinceCommit = 0;

        Client.Commit();
    }

    private long IncrementAssignmentVersion(TopicPartition topicPartition)
    {
        long version = _assignmentVersions.GetValueOrDefault(topicPartition) + 1;
        _assignmentVersions[topicPartition] = version;

        return version;
    }

    private bool IsNotRevoked(TopicPartition topicPartition) => !_revokedPartitions.Contains(topicPartition);

    // Must be called while holding the assignment lock
    private void StartReadingAssignmentCore()
    {
        IReadOnlyList<TopicPartition> assignment = Client.Assignment;

        // A partition whose revocation is still being handled by the stopping loop (unassigned only once its callback returns) isn't
        // caught up with: it's added again if it's assigned again
        TopicPartition[] catchUpPartitions = [.. assignment.Where(partition => !_revokingPartitions.Contains(partition))];

        foreach (TopicPartition partition in assignment)
        {
            IncrementAssignmentVersion(partition);
            _revokedPartitions.Remove(partition);
            _rollingBackPartitions.Remove(partition);

            // The offsets tracked before a stop are kept: the consume loop seeks back to the first uncommitted one before polling
            // again
            _offsets.TrackPartition(partition);
            _channelsManager.StartReading(partition);
        }

        // The dynamic assignment is received by the poll, after the start
        if (assignment.Count > 0 || Configuration.IsStaticAssignment)
            NotifyIfCaughtUp(_catchUp.AddPartitions(catchUpPartitions));

        if (assignment.Count > 0)
            SetConnectedStatus();
    }

    private sealed record RollbackPartition(TopicPartitionOffset Offset, long Version);
}
