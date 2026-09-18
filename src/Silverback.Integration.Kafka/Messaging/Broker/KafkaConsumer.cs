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

    private readonly OffsetsTracker? _offsets; // tracked only when processing partitions together

    private readonly System.Threading.Lock _assignmentLock = new(); // serializes partition state transitions and updates to the commit counter

    private readonly HashSet<TopicPartition> _revokedPartitions = [];

    private readonly Dictionary<TopicPartition, long> _assignmentVersions = [];

    private readonly HashSet<TopicPartition> _rollingBackPartitions = [];

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

        if (!Configuration.ProcessPartitionsIndependently)
            _offsets = new OffsetsTracker();

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
    public void Pause(IEnumerable<TopicPartition> partitions) => Client.Pause(partitions);

    /// <inheritdoc cref="IKafkaConsumer.Resume" />
    public void Resume(IEnumerable<TopicPartition> partitions) => Client.Resume(partitions);

    /// <inheritdoc cref="IKafkaConsumer.Seek" />
    public void Seek(TopicPartitionOffset topicPartitionOffset) => Client.Seek(topicPartitionOffset);

    /// <inheritdoc cref="IKafkaConsumer.GetOffsetsForTimestamps" />
    public IReadOnlyList<TopicPartitionOffset> GetOffsetsForTimestamps(IEnumerable<TopicPartitionTimestamp> topicPartitionTimestamps, TimeSpan timeout) =>
        Client.OffsetsForTimes(topicPartitionTimestamps, timeout);

    internal IReadOnlyCollection<TopicPartitionOffset> OnPartitionsAssigned(IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets)
    {
        if (!IsStartedAndNotStopping())
            return [];

        topicPartitionOffsets = new StoredOffsetsLoader(_offsetStoreFactory, Configuration, ServiceProvider)
            .ApplyStoredOffsets(topicPartitionOffsets);

        lock (_assignmentLock)
        {
            if (!IsStartedAndNotStopping())
                return [];

            foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionOffsets)
            {
                _rollingBackPartitions.Remove(topicPartitionOffset.TopicPartition);
                IncrementAssignmentVersion(topicPartitionOffset.TopicPartition);
                _revokedPartitions.Remove(topicPartitionOffset.TopicPartition);
                _offsets?.TrackOffset(topicPartitionOffset);
                _channelsManager.StartReading(topicPartitionOffset.TopicPartition);
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
            channels = [.. topicPartitionOffsets.Select(offset => _channelsManager.GetChannel(offset.TopicPartition))
                .OfType<PartitionChannel>().Distinct()];

            foreach (TopicPartition partition in revoked)
            {
                _revokedPartitions.Add(partition);
                _rollingBackPartitions.Remove(partition);
                IncrementAssignmentVersion(partition);
            }

            // A shared channel also contains records from retained partitions. Rebuild it once and replay those records.
            if (_offsets != null && topicPartitionOffsets.Count > 0)
            {
                foreach (KafkaOffset offset in _offsets.GetRollbackOffSets().Where(offset => !revoked.Contains(offset.TopicPartition) &&
                                                                                           IsNotRevoked(offset.TopicPartition)))
                {
                    long version = IncrementAssignmentVersion(offset.TopicPartition);
                    _rollingBackPartitions.Add(offset.TopicPartition);
                    retained.Add(new RollbackPartition(offset.AsTopicPartitionOffset(), version));
                }

                if (retained.Count > 0 && IsStartedAndNotStopping())
                    Client.Pause(retained.Select(partition => partition.Offset.TopicPartition));
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
                _offsets?.UntrackPartition(offset.TopicPartition);

            foreach (RollbackPartition partition in retained.Where(IsCurrentRollback))
            {
                if (partition.Offset.Offset != Offset.Unset)
                    Client.Seek(partition.Offset);

                _channelsManager.StartReading(partition.Offset.TopicPartition);
                Client.Resume([partition.Offset.TopicPartition]);
                _rollingBackPartitions.Remove(partition.Offset.TopicPartition);
            }
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
            foreach (TopicPartition partition in Client.Assignment)
            {
                IncrementAssignmentVersion(partition);
                _revokedPartitions.Remove(partition);
                _rollingBackPartitions.Remove(partition);
                _offsets?.UntrackPartition(partition);
                _offsets?.TrackOffset(new KafkaOffset(partition, Offset.Unset));
                _channelsManager.StartReading(partition);
            }

            if (Client.Assignment.Count > 0)
                SetConnectedStatus();
        }

        // Assignment callbacks are delivered by Consume, so polling must start before assignment.
        StartConsumeLoopHandler();
        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StopCoreAsync()" />
    protected override ValueTask StopCoreAsync()
    {
        lock (_assignmentLock)
        {
            foreach (TopicPartition partition in _assignmentVersions.Keys)
                _assignmentVersions[partition]++;
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
            foreach (KafkaOffset offset in brokerMessageIdentifiers.Where(IsOwnedOffset))
            {
                _offsets?.Commit(offset);
                StoreOffset(new TopicPartitionOffset(offset.TopicPartition, offset.Offset + 1));
                stored = true;
            }

            if (stored)
                CommitOffsetsIfNeeded();
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

            // Capture before enumerating offsets: enumeration itself may overlap a rebalance.
            versions = new Dictionary<TopicPartition, long>(_assignmentVersions);
        }

        KafkaOffset[] requestedOffsets = [.. brokerMessageIdentifiers];
        List<RollbackPartition> partitions = [];
        HashSet<PartitionChannel> channels = [];
        lock (_assignmentLock)
        {
            KafkaOffset[] currentOffsets = [.. requestedOffsets.Where(offset =>
                IsCurrentOffset(offset) && versions.TryGetValue(offset.TopicPartition, out long version) &&
                _assignmentVersions.GetValueOrDefault(offset.TopicPartition) == version)];

            // An old shared sequence must never expand its rollback to a replacement channel's offsets.
            if (currentOffsets.Length == 0 || _offsets != null && currentOffsets.Length != requestedOffsets.Length)
            {
                _logger.LogConsumerTrace(this, "Skipping rollback restart for obsolete partition assignments");
                return ValueTask.CompletedTask;
            }

            IEnumerable<KafkaOffset> offsets = _offsets?.GetRollbackOffSets() ?? currentOffsets;
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

            Client.Pause(partitions.Select(partition => partition.Offset.TopicPartition));
        }

        // Stop the captured instances, never whichever channel now happens to occupy their partition keys
        Task stopping = Task.WhenAll(channels.Select(_channelsManager.StopChannelAsync));

        lock (_assignmentLock)
        {
            foreach (RollbackPartition partition in partitions.Where(IsCurrentRollback))
            {
                if (partition.Offset.Offset != Offset.Unset)
                {
                    Client.Seek(partition.Offset);
                    _logger.LogPartitionOffsetReset(partition.Offset, this);
                }
            }
        }

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

    private ValueTask OnClientConnectedAsync(BrokerClient client) => StartAsync();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task RestartChannelsAfterRollbackAsync(Task stopping, IReadOnlyCollection<RollbackPartition> partitions)
    {
        try
        {
            await stopping.ConfigureAwait(false);

            lock (_assignmentLock)
            {
                RollbackPartition[] current = [.. partitions.Where(IsCurrentRollback)];
                if (current.Length != partitions.Count)
                {
                    _logger.LogConsumerTrace(this, "Skipping rollback restart for obsolete partition assignments");
                    if (!Configuration.ProcessPartitionsIndependently)
                        return;
                }

                foreach (RollbackPartition partition in current)
                {
                    // The old channel was removed by StopChannelAsync; no sequence disposal is performed under this lock.
                    _channelsManager.StartReading(partition.Offset.TopicPartition);
                    Client.Resume([partition.Offset.TopicPartition]);
                    _rollingBackPartitions.Remove(partition.Offset.TopicPartition);
                    _logger.LogPartitionResumed(partition.Offset.TopicPartition, this);
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

        _logger.LogConsumerTrace(
            this,
            "ConsumeLoopHandler started | InstanceId: {InstanceId}, TaskId: {TaskId}",
            () => [_consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id]);
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Synchronously called")]
    private async Task WaitUntilConsumeLoopHandlerStopsAsync()
    {
        _logger.LogConsumerTrace(
            this,
            "Waiting ConsumeLoopHandler stop | InstanceId: {InstanceId}, TaskId: {TaskId}",
            () => [_consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id]);

        await _consumeLoopHandler.Stopping.ConfigureAwait(false);

        _logger.LogConsumerTrace(
            this,
            "ConsumeLoopHandler stopped | InstanceId: {InstanceId}, TaskId: {TaskId}.",
            () => [_consumeLoopHandler.Id, _consumeLoopHandler.Stopping.Id]);
    }

    private async Task WaitUntilChannelsManagerStopsAsync()
    {
        _logger.LogConsumerTrace(this, "Waiting ChannelsManager stop");

        await _channelsStopping.ConfigureAwait(false);

        _logger.LogConsumerTrace(this, "ChannelsManager stopped");
    }

    private void StoreOffset(TopicPartitionOffset offset)
    {
        _logger.LogConsumerTrace(
            this,
            "Storing offset {Topic}[{Partition}]@{Offset}",
            () => [offset.Topic, offset.Partition.Value, offset.Offset.Value]);

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

    private sealed record RollbackPartition(TopicPartitionOffset Offset, long Version);
}
