// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerConfiguration, KafkaOffset>
{
    private readonly IConfluentConsumerBuilder _confluentConsumerBuilder;

    private readonly IBrokerCallbacksInvoker _callbacksInvoker;

    private readonly IInboundLogger<KafkaConsumer> _logger;

    private readonly object _messagesSinceCommitLock = new();

    private readonly IKafkaMessageSerializer _serializer;

    [SuppressMessage("", "CA2213", Justification = "False positive: DisposeAsync is being called")]
    private readonly KafkaSequenceStoreCollection _kafkaSequenceStoreCollection;

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConsumeLoopHandler _consumeLoopHandler;

    private readonly Dictionary<TopicPartition, KafkaConsumerEndpoint> _endpointsCache = new();

    private readonly Func<TopicPartition, KafkaConsumerEndpoint> _endpointFactory;

    [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
    private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

    private int _messagesSinceCommit;

    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumer" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that is instantiating the consumer.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="KafkaConsumerConfiguration"/>.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="confluentConsumerBuilder">
    ///     The <see cref="IConfluentConsumerBuilder" />.
    /// </param>
    /// <param name="callbacksInvoker">
    ///     The <see cref="IBrokerCallbacksInvoker" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IInboundLogger{TCategoryName}" />.
    /// </param>
    public KafkaConsumer(
        KafkaBroker broker,
        KafkaConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IConfluentConsumerBuilder confluentConsumerBuilder,
        IBrokerCallbacksInvoker callbacksInvoker,
        IServiceProvider serviceProvider,
        IInboundLogger<KafkaConsumer> logger)
        : base(broker, configuration, behaviorsProvider, serviceProvider, logger)
    {
        Check.NotNull(configuration, nameof(configuration));
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        _confluentConsumerBuilder = Check.NotNull(confluentConsumerBuilder, nameof(confluentConsumerBuilder));
        _confluentConsumerBuilder.SetConfig(configuration.Client.GetConfluentConfig());
        _confluentConsumerBuilder.SetEventsHandlers(this, callbacksInvoker, logger);

        _serializer = configuration.Serializer as IKafkaMessageSerializer ?? new DefaultKafkaMessageSerializer(configuration.Serializer);

        _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
        _logger = Check.NotNull(logger, nameof(logger));

        _kafkaSequenceStoreCollection = new KafkaSequenceStoreCollection(serviceProvider, Configuration.ProcessPartitionsIndependently);
        _channelsManager = new ConsumerChannelsManager(this, _callbacksInvoker, logger);
        _consumeLoopHandler = new ConsumeLoopHandler(this, _channelsManager, _logger);

        _endpointFactory = topicPartition => new KafkaConsumerEndpoint(topicPartition, configuration);
    }

    /// <summary>
    ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
    /// </summary>
    public string MemberId => _confluentConsumer?.MemberId ?? string.Empty;

    /// <summary>
    ///     Gets the current partition assignment.
    /// </summary>
    public IReadOnlyList<TopicPartition>? PartitionAssignment => _confluentConsumer?.Assignment;

    internal IConsumer<byte[]?, byte[]?> ConfluentConsumer =>
        _confluentConsumer ?? throw new InvalidOperationException("ConfluentConsumer not set.");

    internal void OnPartitionsAssigned(IReadOnlyList<TopicPartition> topicPartitions)
    {
        if (IsDisconnecting)
            return;

        IsConsuming = true;

        StartChannelsManager(topicPartitions);

        SetReadyStatus();
    }

    internal void OnPartitionsRevoked(IReadOnlyList<TopicPartitionOffset> topicPartitionOffsets)
    {
        IEnumerable<TopicPartition> topicPartitions = topicPartitionOffsets.Select(offset => offset.TopicPartition);

        RevertReadyStatus();

        _consumeLoopHandler.StopAsync().FireAndForget();

        AsyncHelper.RunSynchronously(
            () => topicPartitions.ParallelForEachAsync(
                async topicPartition =>
                {
                    await _channelsManager.StopReadingAsync(topicPartition).ConfigureAwait(false);

                    await _kafkaSequenceStoreCollection
                        .GetSequenceStore(topicPartition)
                        .AbortAllAsync(SequenceAbortReason.ConsumerAborted)
                        .ConfigureAwait(false);
                }));

        if (!Configuration.Client.IsAutoCommitEnabled)
            CommitOffsets();

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
        if (Configuration.Client.EnableAutoRecovery)
        {
            _logger.LogPollTimeoutAutoRecovery(logMessage, this);
            TriggerReconnectAsync().FireAndForget();
        }
        else
        {
            _logger.LogPollTimeoutNoAutoRecovery(logMessage, this);
            RevertReadyStatus();
        }

        return true;
    }

    internal async Task HandleMessageAsync(
        Message<byte[]?, byte[]?> message,
        TopicPartitionOffset topicPartitionOffset)
    {
        MessageHeaderCollection headers = new(message.Headers.ToSilverbackHeaders());

        KafkaConsumerEndpoint endpoint = _endpointsCache.GetOrAdd(topicPartitionOffset.TopicPartition, _endpointFactory);

        if (message.Key != null)
        {
            string deserializedKafkaKey = _serializer.DeserializeKey(message.Key, headers, endpoint);
            headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, deserializedKafkaKey);
            headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deserializedKafkaKey);
        }

        headers.AddOrReplace(
            KafkaMessageHeaders.TimestampKey,
            message.Timestamp.UtcDateTime.ToString("O"));

        await HandleMessageAsync(
                message.Value,
                headers,
                endpoint,
                new KafkaOffset(topicPartitionOffset))
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.InitSequenceStore" />
    protected override ISequenceStoreCollection InitSequenceStore() => _kafkaSequenceStoreCollection;

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.ConnectCoreAsync" />
    protected override Task ConnectCoreAsync()
    {
        InitConfluentConsumer();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.DisconnectCoreAsync" />
    protected override Task DisconnectCoreAsync()
    {
        if (!Configuration.Client.IsAutoCommitEnabled)
            CommitOffsets();

        DisposeConfluentConsumer();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.StartCoreAsync" />
    protected override Task StartCoreAsync()
    {
        // Set IsConsuming before calling the init methods, otherwise they don't start for real
        IsConsuming = true;

        // Start reading from the channels right away in case of static partitions assignment or if
        // the consumer is restarting and the partition assignment is set already
        if (Configuration.IsStaticAssignment ||
            _confluentConsumer?.Assignment != null && _confluentConsumer.Assignment.Count > 0)
        {
            StartChannelsManager(ConfluentConsumer.Assignment);
        }

        // The consume loop must start immediately because the partitions assignment is received
        // only after Consume is called once
        StartConsumeLoopHandler();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.StopCoreAsync" />
    protected override Task StopCoreAsync()
    {
        _consumeLoopHandler.StopAsync().FireAndForget();
        _channelsManager.StopReadingAsync().FireAndForget();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override Task WaitUntilConsumingStoppedCoreAsync() =>
        Task.WhenAll(
            WaitUntilChannelsManagerStopsAsync(),
            WaitUntilConsumeLoopHandlerStopsAsync());

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.CommitCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override Task CommitCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        IEnumerable<TopicPartitionOffset> lastOffsets = brokerMessageIdentifiers
            .GroupBy(offset => offset.Key)
            .Select(
                offsetsGroup => offsetsGroup
                    .OrderByDescending(offset => offset.Offset)
                    .First()
                    .AsTopicPartitionOffset());

        StoreOffset(
            lastOffsets
                .Select(
                    topicPartitionOffset => new TopicPartitionOffset(
                        topicPartitionOffset.TopicPartition,
                        topicPartitionOffset.Offset + 1)) // Commit next offset (+1)
                .ToArray());

        CommitOffsetsIfNeeded();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.RollbackCoreAsync(IReadOnlyCollection{TIdentifier})" />
    protected override Task RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
    {
        List<TopicPartitionOffset> latestTopicPartitionOffsets =
            brokerMessageIdentifiers
                .GroupBy(offset => offset.Key)
                .Select(
                    offsetsGroup => offsetsGroup
                        .OrderBy(offset => offset.Offset)
                        .First()
                        .AsTopicPartitionOffset())
                .ToList();

        if (IsConsuming)
            _confluentConsumer?.Pause(latestTopicPartitionOffsets.Select(offset => offset.TopicPartition));

        List<Task?> channelsManagerStoppingTasks = new(latestTopicPartitionOffsets.Count);

        foreach (TopicPartitionOffset? topicPartitionOffset in latestTopicPartitionOffsets)
        {
            channelsManagerStoppingTasks.Add(_channelsManager.StopReadingAsync(topicPartitionOffset.TopicPartition));
            ConfluentConsumer.Seek(topicPartitionOffset);
            _logger.LogPartitionOffsetReset(topicPartitionOffset, this);
        }

        Task.Run(
                () => RestartConsumeLoopAfterRollbackAsync(
                    channelsManagerStoppingTasks,
                    latestTopicPartitionOffsets))
            .FireAndForget();

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _disposed)
            return;

        _consumeLoopHandler.Dispose();
        _channelsManager.Dispose();
        AsyncHelper.RunSynchronously(() => _kafkaSequenceStoreCollection.DisposeAsync().AsTask());

        _disposed = true;
    }

    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    private async Task RestartConsumeLoopAfterRollbackAsync(
        IEnumerable<Task?> channelsManagerStoppingTasks,
        IReadOnlyCollection<TopicPartitionOffset> latestTopicPartitionOffsets)
    {
        try
        {
            await Task.WhenAll(channelsManagerStoppingTasks.Where(task => task != null))
                .ConfigureAwait(false);

            IEnumerable<TopicPartition> topicPartitions = latestTopicPartitionOffsets.Select(offset => offset.TopicPartition);

            foreach (TopicPartition? topicPartition in topicPartitions)
            {
                _channelsManager?.Reset(topicPartition);

                if (IsConsuming)
                {
                    _channelsManager?.StartReading(topicPartition);
                    _confluentConsumer?.Resume(new[] { topicPartition });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogConsumerStartError(this, ex);

            // Try to recover from error
            await TriggerReconnectAsync().ConfigureAwait(false);
        }
    }

    private void InitConfluentConsumer()
    {
        _confluentConsumer = _confluentConsumerBuilder.Build();

        List<TopicPartitionOffset> staticPartitionAssignment =
            Configuration.TopicPartitions.Where(topicPartition => topicPartition.Partition != Partition.Any).ToList();

        IReadOnlyCollection<string> anyPartitionTopics =
            Configuration.TopicPartitions
                .Where(topicPartition => topicPartition.Partition == Partition.Any)
                .Select(topicPartition => topicPartition.Topic)
                .ToList();

        if (staticPartitionAssignment.Count > 0 &&
            anyPartitionTopics.Count > 0 &&
            Configuration.PartitionOffsetsProvider == null)
        {
            throw new InvalidOperationException(
                "Cannot mix static partition assignments and subscriptions in the same consumer." +
                "A PartitionOffsetsProvider is required when mixing Partition.Any with static partition assignments.");
        }

        if (Configuration.PartitionOffsetsProvider != null && anyPartitionTopics.Count > 0)
        {
            using IAdminClient adminClient = ServiceProvider
                .GetRequiredService<IConfluentAdminClientBuilder>()
                .Build(Configuration.Client.GetConfluentConfig());

            List<TopicPartition> availablePartitions = Configuration.TopicPartitions
                .Where(topicPartition => topicPartition.Partition == Partition.Any) // TODO: Test
                .SelectMany(
                    topicPartition =>
                        adminClient
                            .GetMetadata(topicPartition.Topic, TimeSpan.FromMinutes(5)) // TODO: 5 minutes timeout?
                            .Topics[0]
                            .Partitions
                            .Select(metadata => new TopicPartition(topicPartition.Topic, metadata.PartitionId)))
                .ToList();

            staticPartitionAssignment.AddRange(Configuration.PartitionOffsetsProvider(availablePartitions));
        }

        if (staticPartitionAssignment.Count > 0)
        {
            _confluentConsumer.Assign(staticPartitionAssignment);

            staticPartitionAssignment.ForEach(topicPartitionOffset => _logger.LogPartitionManuallyAssigned(topicPartitionOffset, this));

            SetReadyStatus();
        }
        else
        {
            _confluentConsumer.Subscribe(anyPartitionTopics);
        }
    }

    private void StartConsumeLoopHandler()
    {
        if (!IsConsuming || IsStopping)
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

    private void StartChannelsManager(IEnumerable<TopicPartition> topicPartitions)
    {
        if (!IsConsuming || IsStopping)
            return;

        _channelsManager.StartReading(topicPartitions);
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

    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    private void DisposeConfluentConsumer()
    {
        try
        {
            _confluentConsumer?.Close();
            _confluentConsumer?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        catch (Exception ex)
        {
            _logger.LogConfluentConsumerDisconnectError(this, ex);
        }

        _confluentConsumer = null;
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
            ConfluentConsumer.StoreOffset(offset);
        }
    }

    private void CommitOffsetsIfNeeded()
    {
        if (Configuration.Client.IsAutoCommitEnabled)
            return;

        lock (_messagesSinceCommitLock)
        {
            if (++_messagesSinceCommit < Configuration.Client.CommitOffsetEach)
                return;

            _messagesSinceCommit = 0;
        }

        ConfluentConsumer.Commit();
    }

    private void CommitOffsets()
    {
        if (_messagesSinceCommit == 0)
            return;

        try
        {
            List<TopicPartitionOffset>? offsets = ConfluentConsumer.Commit();

            _callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                handler => handler.OnOffsetsCommitted(
                    new CommittedOffsets(
                        offsets.Select(
                                offset =>
                                    new TopicPartitionOffsetError(
                                        offset,
                                        new Error(ErrorCode.NoError)))
                            .ToList(),
                        new Error(ErrorCode.NoError)),
                    this));
        }
        catch (TopicPartitionOffsetException ex)
        {
            _callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(handler => handler.OnOffsetsCommitted(new CommittedOffsets(ex.Results, ex.Error), this));

            throw;
        }
        catch (KafkaException ex)
        {
            _callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(handler => handler.OnOffsetsCommitted(new CommittedOffsets(null, ex.Error), this));
        }
    }
}
