// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TIdentifier}" />
public class KafkaConsumer : Consumer<KafkaOffset>
{
    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly IConsumerLogger<KafkaConsumer> _logger;

    private readonly object _messagesSinceCommitLock = new();

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConsumeLoopHandler _consumeLoopHandler;

    private readonly KafkaConsumerEndpointsCache _endpointsCache;

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
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
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
        IConsumerLogger<KafkaConsumer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).Endpoints,
            behaviorsProvider,
            new KafkaSequenceStoreCollection(serviceProvider, configuration.ProcessPartitionsIndependently),
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));

        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _logger = Check.NotNull(logger, nameof(logger));

        _channelsManager = new ConsumerChannelsManager(this, callbacksInvoker, logger);
        _consumeLoopHandler = new ConsumeLoopHandler(this, _channelsManager, _logger);

        _endpointsCache = new KafkaConsumerEndpointsCache(configuration);

        Client.Consumer = this;
        Client.Initialized.AddHandler(OnClientConnectedAsync);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Client" />
    public new IConfluentConsumerWrapper Client { get; }

    /// <summary>
    ///     Gets the consumer configuration.
    /// </summary>
    public KafkaConsumerConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    public new IReadOnlyCollection<KafkaConsumerEndpointConfiguration> EndpointsConfiguration => Configuration.Endpoints;

    /// <summary>
    ///     Pauses the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    // TODO: Test
    public void Pause(IEnumerable<TopicPartition> partitions) => Client.Pause(partitions);

    /// <summary>
    ///     Resumes the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    // TODO: Test
    public void Resume(IEnumerable<TopicPartition> partitions) => Client.Resume(partitions);

    /// <summary>
    ///     Seeks the specified partition to the specified offset.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset.
    /// </param>
    // TODO: Test
    public void Seek(TopicPartitionOffset topicPartitionOffset) => Client.Seek(topicPartitionOffset);

    internal IReadOnlyCollection<TopicPartitionOffset> OnPartitionsAssigned(IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets)
    {
        if (!IsStartedAndNotStopping())
            return Array.Empty<TopicPartitionOffset>(); // TODO: Check this

        topicPartitionOffsets = new StoredOffsetsLoader(_offsetStoreFactory, Configuration).ApplyStoredOffsets(topicPartitionOffsets);

        _channelsManager.StartReading(topicPartitionOffsets.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition));

        SetConnectedStatus();

        return topicPartitionOffsets;
    }

    internal void OnPartitionsRevoked(IReadOnlyList<TopicPartitionOffset> topicPartitionOffsets)
    {
        IEnumerable<TopicPartition> topicPartitions = topicPartitionOffsets.Select(offset => offset.TopicPartition);

        RevertConnectedStatus();

        _consumeLoopHandler.StopAsync().FireAndForget();

        AsyncHelper.RunSynchronously(
            () => topicPartitions.ParallelForEachAsync(
                async topicPartition =>
                {
                    await _channelsManager.StopReadingAsync(topicPartition).ConfigureAwait(false);

                    await ((KafkaSequenceStoreCollection)SequenceStores)
                        .GetSequenceStore(topicPartition)
                        .AbortAllAsync(SequenceAbortReason.ConsumerAborted)
                        .ConfigureAwait(false);
                }));

        if (!Configuration.EnableAutoCommit)
            Client.Commit();

        // The ConsumeLoopHandler needs to be immediately restarted because the partitions will be
        // reassigned only if Consume is called again.
        // Furthermore the ConsumeLoopHandler stopping Task cannot be awaited in the
        // OnPartitionsRevoked callback, before the partitions are revoked because the Consume method is
        // "frozen" during that operation and will never return, therefore the stopping Task would never
        // complete. Therefore, let's start an async Task to await it and restart the ChannelManager.
        Task.Run(RestartConsumeLoopHandlerAsync).FireAndForget();
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
        TopicPartitionOffset topicPartitionOffset)
    {
        MessageHeaderCollection headers = new(message.Headers.ToSilverbackHeaders());

        (KafkaConsumerEndpoint endpoint, IKafkaMessageSerializer serializer) = _endpointsCache.GetEndpoint(topicPartitionOffset.TopicPartition);

        if (message.Key != null)
        {
            string deserializedKafkaKey = serializer.DeserializeKey(message.Key, headers, endpoint);
            headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, deserializedKafkaKey);
            headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deserializedKafkaKey);
        }

        headers.AddOrReplace(KafkaMessageHeaders.Timestamp, message.Timestamp.UtcDateTime.ToString("O"));

        await HandleMessageAsync(
                message.Value,
                headers,
                endpoint,
                new KafkaOffset(topicPartitionOffset))
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StartCoreAsync" />
    protected override ValueTask StartCoreAsync()
    {
        // Start reading from the channels right away in case of static partitions assignment or if
        // the consumer is restarting and the partition assignment is set already
        if (Configuration.IsStaticAssignment || Client.Assignment.Count > 0)
        {
            _channelsManager.StartReading(Client.Assignment);
            SetConnectedStatus();
        }

        // The consume loop must start immediately because the partitions assignment is received
        // only after Consume is called once
        StartConsumeLoopHandler();

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StopCoreAsync" />
    protected override ValueTask StopCoreAsync()
    {
        _consumeLoopHandler.StopAsync().FireAndForget();
        _channelsManager.StopReadingAsync().FireAndForget();

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override async ValueTask WaitUntilConsumingStoppedCoreAsync() =>
        await Task.WhenAll(WaitUntilChannelsManagerStopsAsync(), WaitUntilConsumeLoopHandlerStopsAsync()).ConfigureAwait(false);

    /// <inheritdoc cref="Consumer{TIdentifier}.CommitCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override ValueTask CommitCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        StoreOffset(
            brokerMessageIdentifiers
                .Select(
                    topicPartitionOffset => new TopicPartitionOffset(
                        topicPartitionOffset.TopicPartition,
                        topicPartitionOffset.Offset + 1)) // Commit next offset (+1)
                .ToArray());

        CommitOffsetsIfNeeded();

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.RollbackCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override ValueTask RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        if (IsStarted)
            Client.Pause(brokerMessageIdentifiers.Select(offset => offset.TopicPartition));

        List<Task?> channelsManagerStoppingTasks = new(brokerMessageIdentifiers.Count);

        IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets = brokerMessageIdentifiers
            .Select(offset => offset.AsTopicPartitionOffset())
            .AsReadOnlyList();

        foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionOffsets)
        {
            channelsManagerStoppingTasks.Add(_channelsManager.StopReadingAsync(topicPartitionOffset.TopicPartition));
            Client.Seek(topicPartitionOffset);
            _logger.LogPartitionOffsetReset(topicPartitionOffset, this);
        }

        Task.Run(() => RestartConsumeLoopAfterRollbackAsync(channelsManagerStoppingTasks, topicPartitionOffsets)).FireAndForget();

        return default;
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
    [SuppressMessage("ReSharper", "RedundantSuppressNullableWarningExpression", Justification = "Needed to avoid other false positives")]
    private async Task RestartConsumeLoopAfterRollbackAsync(
        IEnumerable<Task?> channelsManagerStoppingTasks,
        IReadOnlyCollection<TopicPartitionOffset> latestTopicPartitionOffsets)
    {
        try
        {
            await Task.WhenAll(channelsManagerStoppingTasks.Where(task => task != null)!)
                .ConfigureAwait(false);

            IEnumerable<TopicPartition> topicPartitions = latestTopicPartitionOffsets.Select(offset => offset.TopicPartition);

            foreach (TopicPartition? topicPartition in topicPartitions)
            {
                _channelsManager?.Reset(topicPartition);

                if (!IsStarted)
                    continue;

                _channelsManager?.StartReading(topicPartition);
                Client.Resume(new[] { topicPartition });
            }
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStartError(this, ex);

            // Try to recover from error
            await TriggerReconnectAsync().ConfigureAwait(false);
        }
    }

    private void StartConsumeLoopHandler()
    {
        if (!(IsStarted || IsStarting) || IsStopping)
            return;

        _consumeLoopHandler.Start();

        _logger.LogConsumerLowLevelTrace(
            this,
            "ConsumeLoopHandler started. | instanceId: {instanceId}, taskId: {taskId}",
            () => new object[]
            {
                _consumeLoopHandler.Id,
                _consumeLoopHandler.Stopping.Id
            });
    }

    private async Task RestartConsumeLoopHandlerAsync()
    {
        try
        {
            await WaitUntilConsumeLoopHandlerStopsAsync().ConfigureAwait(false);

            StartConsumeLoopHandler();
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStartError(this, ex);
            throw;
        }
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Synchronously called")]
    private async Task WaitUntilConsumeLoopHandlerStopsAsync()
    {
        if (_consumeLoopHandler == null)
        {
            _logger.LogConsumerLowLevelTrace(this, "ConsumeLoopHandler is null.");
            return;
        }

        _logger.LogConsumerLowLevelTrace(
            this,
            "Waiting until ConsumeLoopHandler stops... | instanceId: {instanceId}, taskId: {taskId}",
            () => new object[]
            {
                _consumeLoopHandler.Id,
                _consumeLoopHandler.Stopping.Id
            });
        await _consumeLoopHandler.Stopping.ConfigureAwait(false);
        _logger.LogConsumerLowLevelTrace(
            this,
            "ConsumeLoopHandler stopped | instanceId: {instanceId}, taskId: {taskId}.",
            () => new object[]
            {
                _consumeLoopHandler.Id,
                _consumeLoopHandler.Stopping.Id
            });
    }

    private async Task WaitUntilChannelsManagerStopsAsync()
    {
        _logger.LogConsumerLowLevelTrace(this, "Waiting until ChannelsManager stops...");
        await _channelsManager.Stopping.ConfigureAwait(false);
        _logger.LogConsumerLowLevelTrace(this, "ChannelsManager stopped.");
    }

    private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
    {
        foreach (TopicPartitionOffset? offset in offsets)
        {
            _logger.LogConsumerLowLevelTrace(
                this,
                "Storing offset {topic}[{partition}]@{offset}.",
                () => new object[]
                {
                    offset.Topic,
                    offset.Partition.Value,
                    offset.Offset.Value
                });
            Client.StoreOffset(offset);
        }
    }

    private void CommitOffsetsIfNeeded()
    {
        if (Configuration.EnableAutoCommit)
            return;

        lock (_messagesSinceCommitLock)
        {
            if (++_messagesSinceCommit < Configuration.CommitOffsetEach)
                return;

            _messagesSinceCommit = 0;

            Client.Commit();
        }
    }
}
