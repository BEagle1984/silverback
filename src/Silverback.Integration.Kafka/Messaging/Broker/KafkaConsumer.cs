// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TIdentifier}" />
public class KafkaConsumer : Consumer<KafkaOffset>, IKafkaConsumer
{
    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly ISilverbackLogger<KafkaConsumer> _logger;

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConsumeLoopHandler _consumeLoopHandler;

    private readonly KafkaConsumerEndpointsCache _endpointsCache;

    // Tracks the polled and committed offsets of every partition: the first uncommitted offset is where a restart seeks back to.
    // When processing the partitions together, it also drives the rollback and the cooperative revocation replay.
    private readonly OffsetsTracker _offsets = new();

    private readonly System.Threading.Lock _assignmentLock = new(); // serializes partition state transitions and updates to the commit counter

    private readonly HashSet<TopicPartition> _revokedPartitions = [];

    private readonly Dictionary<TopicPartition, long> _assignmentVersions = [];

    private readonly HashSet<TopicPartition> _rollingBackPartitions = [];

    // The partitions paused to roll back their records (or to replay the ones retained by a revocation) and not resumed yet: when
    // a stop or a rebalance defeats the rollback restart, they are resumed by the next start or when they are assigned again
    private readonly HashSet<TopicPartition> _rollbackPausedPartitions = [];

    // The last offset whose commit failed in each partition, when a client side offset store is used: the offsets are stored before
    // committing, so the rollback must not consume those records again
    private readonly Dictionary<TopicPartition, Offset> _commitFailedOffsets = [];

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
            Check.NotNull(configuration, nameof(configuration)).Endpoints,
            behaviorsProvider,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));

        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _logger = Check.NotNull(logger, nameof(logger));

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
        Check.NotNull(partitions, nameof(partitions));

        lock (_assignmentLock)
        {
            TopicPartition[] pausedPartitions = [.. partitions];
            Client.Pause(pausedPartitions);

            // The partitions paused by the user must stay paused when a restart resumes the ones paused by a defeated rollback
            _rollbackPausedPartitions.ExceptWith(pausedPartitions);
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.Resume" />
    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        Check.NotNull(partitions, nameof(partitions));

        lock (_assignmentLock)
        {
            TopicPartition[] resumedPartitions = [.. partitions];
            Client.Resume(resumedPartitions);

            // Not paused anymore: neither a restart nor a new assignment has to resume them
            _rollbackPausedPartitions.ExceptWith(resumedPartitions);
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.Seek" />
    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset));

        lock (_assignmentLock)
        {
            Client.Seek(topicPartitionOffset);

            // The new position replaces the tracked one, so that a restart doesn't seek back to the records skipped on purpose.
            // A seek while the partition is being consumed mixes the two positions anyway: the records polled before it may
            // still be tracked, processed and committed afterwards.
            _offsets.UntrackPartition(topicPartitionOffset.TopicPartition);
            _offsets.TrackOffset(topicPartitionOffset);
            _commitFailedOffsets.Remove(topicPartitionOffset.TopicPartition);
        }
    }

    /// <inheritdoc cref="IKafkaConsumer.GetOffsetsForTimestamps" />
    public IReadOnlyList<TopicPartitionOffset> GetOffsetsForTimestamps(IEnumerable<TopicPartitionTimestamp> topicPartitionTimestamps, TimeSpan timeout) =>
        Client.OffsetsForTimes(topicPartitionTimestamps, timeout);

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
                IncrementAssignmentVersion(topicPartitionOffset.TopicPartition);
                _revokedPartitions.Remove(topicPartitionOffset.TopicPartition);
                _offsets.TrackOffset(topicPartitionOffset);
                _channelsManager.StartReading(topicPartitionOffset.TopicPartition);
            }

            // The native pause survives the revocation and the new assignment of a partition (with both the eager and the
            // cooperative assignors): resume the partitions paused by a rollback that the rebalance defeated, or they would never be
            // fetched again. Resumed before the assignment is applied, they are fetched from the assigned offsets only. A native
            // consumer that is still initializing has no paused partitions.
            if (_rollbackPausedPartitions.Count > 0 && Client.Status == ClientStatus.Initialized)
            {
                HashSet<TopicPartition> assignedPartitions = [.. topicPartitionOffsets.Select(offset => offset.TopicPartition)];
                ResumeRollbackPausedPartitionsCore(assignedPartitions.Contains);
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
                _rollingBackPartitions.Remove(partition);
                _commitFailedOffsets.Remove(partition);
                IncrementAssignmentVersion(partition);
            }

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

            foreach (TopicPartitionOffset offset in topicPartitionOffsets)
            {
                _offsets.UntrackPartition(offset.TopicPartition);
            }

            foreach (TopicPartitionOffset rollbackOffset in retained.Where(IsCurrentRollback).Select(partition => partition.Offset))
            {
                if (rollbackOffset.Offset != Offset.Unset)
                    Client.Seek(rollbackOffset);

                _channelsManager.StartReading(rollbackOffset.TopicPartition);
                Client.Resume([rollbackOffset.TopicPartition]);
                _rollbackPausedPartitions.Remove(rollbackOffset.TopicPartition);
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
    ///     Resumes the assigned partitions that were paused by a rollback whose restart was defeated by a stop: they would
    ///     otherwise never be fetched again.
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
            if (_rollbackPausedPartitions.Count == 0 || !IsStartedAndNotStopping() || Client.Status != ClientStatus.Initialized)
                return;

            HashSet<TopicPartition> assignment = [.. Client.Assignment];

            // A partition being rolled back again is resumed by the rollback itself
            ResumeRollbackPausedPartitionsCore(partition =>
                assignment.Contains(partition) && IsNotRevoked(partition) && !_rollingBackPartitions.Contains(partition));
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

    internal async Task HandleMessageAsync(
        Message<byte[]?, byte[]?> message,
        TopicPartitionOffset topicPartitionOffset,
        ISequenceStore sequenceStore,
        Guid sourceChannelInstanceId)
    {
        MessageHeaderCollection headers = [.. message.Headers.ToSilverbackHeaders()];

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
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        lock (_assignmentLock)
        {
            // A non-blocking stop marks the consumer stopped before its completed batches finish committing.
            // Channel ownership remains valid until draining removes the channel.
            bool stored = false;

            try
            {
                foreach (KafkaOffset offset in brokerMessageIdentifiers.Where(IsOwnedOffset))
                {
                    // Tracked as committed only once stored: a record whose offset couldn't be stored must still be consumed again
                    // after a restart (or a shared rollback)
                    StoreOffset(new TopicPartitionOffset(offset.TopicPartition, offset.Offset + 1));
                    _offsets.Commit(offset);
                    stored = true;
                }

                if (stored)
                    CommitOffsetsIfNeeded();
            }
            catch (Exception) when (Configuration.ClientSideOffsetStore != null)
            {
                foreach (KafkaOffset offset in brokerMessageIdentifiers.Where(IsOwnedOffset))
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
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

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

            IEnumerable<KafkaOffset> offsets = Configuration.ProcessPartitionsIndependently
                ? currentOffsets
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

    private ValueTask OnClientConnectedAsync(BrokerClient client)
    {
        // The native consumer was just (re)created: nothing tracked before can be consumed again from it, the new consumer resumes
        // from the committed offsets and none of its partitions is paused
        lock (_assignmentLock)
        {
            _offsets.Clear();
            _rollbackPausedPartitions.Clear();
            _commitFailedOffsets.Clear();
        }

        return StartAsync();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
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

                foreach (TopicPartitionOffset rollbackOffset in current.Select(partition => partition.Offset))
                {
                    TopicPartitionOffset offset = rollbackOffset;

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
                    Client.Resume([rollbackOffset.TopicPartition]);
                    _rollbackPausedPartitions.Remove(rollbackOffset.TopicPartition);
                    _rollingBackPartitions.Remove(rollbackOffset.TopicPartition);
                    _logger.LogPartitionResumed(rollbackOffset.TopicPartition, this);
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
        }

        return nextOffset;
    }

    // Must be called while holding the assignment lock
    private void ResumeRollbackPausedPartitionsCore(Func<TopicPartition, bool> predicate)
    {
        TopicPartition[] partitions = [.. _rollbackPausedPartitions.Where(predicate)];

        if (partitions.Length == 0)
            return;

        Client.Resume(partitions);
        _rollbackPausedPartitions.ExceptWith(partitions);

        foreach (TopicPartition partition in partitions)
        {
            _logger.LogPartitionResumed(partition, this);
        }
    }

    private bool IsCurrentRollback(RollbackPartition partition) =>
        IsStartedAndNotStopping() && IsNotRevoked(partition.Offset.TopicPartition) &&
        _rollingBackPartitions.Contains(partition.Offset.TopicPartition) &&
        _assignmentVersions.GetValueOrDefault(partition.Offset.TopicPartition) == partition.Version;

    private bool IsCurrentOffset(KafkaOffset offset) =>
        IsStartedAndNotStopping() && IsOwnedOffset(offset);

    private bool IsOwnedOffset(KafkaOffset offset) =>
        IsNotRevoked(offset.TopicPartition) && !_rollingBackPartitions.Contains(offset.TopicPartition) &&
        _channelsManager.GetChannel(offset.TopicPartition) is { } channel &&
        (!offset.HasSourceChannel || offset.BelongsToChannel(channel.InstanceId));

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

        if (assignment.Count > 0)
            SetConnectedStatus();
    }

    private sealed record RollbackPartition(TopicPartitionOffset Offset, long Version);
}
