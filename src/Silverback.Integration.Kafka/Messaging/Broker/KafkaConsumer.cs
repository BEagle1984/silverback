﻿// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint, KafkaOffset>
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
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
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
            KafkaConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IConfluentConsumerBuilder confluentConsumerBuilder,
            IBrokerCallbacksInvoker callbacksInvoker,
            IServiceProvider serviceProvider,
            IInboundLogger<KafkaConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _confluentConsumerBuilder = Check.NotNull(
                confluentConsumerBuilder,
                nameof(confluentConsumerBuilder));
            _confluentConsumerBuilder.SetConfig(endpoint.Configuration.GetConfluentConfig());
            _confluentConsumerBuilder.SetEventsHandlers(this, callbacksInvoker, logger);

            _serializer = endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(endpoint.Serializer);

            _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
            _logger = Check.NotNull(logger, nameof(logger));

            _kafkaSequenceStoreCollection = new KafkaSequenceStoreCollection(
                serviceProvider,
                Endpoint.ProcessPartitionsIndependently);
            _channelsManager = new ConsumerChannelsManager(this, _callbacksInvoker, logger);
            _consumeLoopHandler = new ConsumeLoopHandler(this, _channelsManager, _logger);
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

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        internal void OnPartitionsRevoked(IReadOnlyList<TopicPartitionOffset> topicPartitionOffsets)
        {
            IEnumerable<TopicPartition> topicPartitions = topicPartitionOffsets.Select(offset => offset.TopicPartition);

            RevertReadyStatus();

            _consumeLoopHandler.StopAsync();

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

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets();

            // The ConsumeLoopHandler needs to be immediately restarted because the partitions will be
            // reassigned only if Consume is called again.
            // Furthermore the ConsumeLoopHandler stopping Task cannot be awaited in the
            // OnPartitionsRevoked callback, before the partitions are revoked because the Consume method is
            // "frozen" during that operation and will never return, therefore the stopping Task would never
            // complete. Therefore, let's start an async Task to await it and restart the ChannelManager.
            Task.Run(RestartConsumeLoopHandlerAsync);
        }

        internal bool OnPollTimeout(LogMessage logMessage)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
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
            var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

            if (message.Key != null)
            {
                string deserializedKafkaKey = _serializer.DeserializeKey(
                    message.Key,
                    headers,
                    new MessageSerializationContext(Endpoint, topicPartitionOffset.Topic));

                headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, deserializedKafkaKey);
                headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deserializedKafkaKey);
            }

            headers.AddOrReplace(
                KafkaMessageHeaders.TimestampKey,
                message.Timestamp.UtcDateTime.ToString("O"));

            await HandleMessageAsync(
                    message.Value,
                    headers,
                    topicPartitionOffset.Topic,
                    new KafkaOffset(topicPartitionOffset))
                .ConfigureAwait(false);
        }

        /// <inheritdoc cref="Consumer.InitSequenceStore" />
        protected override ISequenceStoreCollection InitSequenceStore() => _kafkaSequenceStoreCollection;

        /// <inheritdoc cref="Consumer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync()
        {
            InitConfluentConsumer();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync()
        {
            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets();

            DisposeConfluentConsumer();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StartCoreAsync" />
        protected override Task StartCoreAsync()
        {
            // Set IsConsuming before calling the init methods, otherwise they don't start for real
            IsConsuming = true;

            // Start reading from the channels right away in case of static partitions assignment or if
            // the consumer is restarting and the partition assignment is set already
            if (Endpoint.TopicPartitions != null ||
                _confluentConsumer?.Assignment != null && _confluentConsumer.Assignment.Count > 0)
            {
                StartChannelsManager(ConfluentConsumer.Assignment);
            }

            // The consume loop must start immediately because the partitions assignment is received
            // only after Consume is called once
            StartConsumeLoopHandler();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            _consumeLoopHandler.StopAsync().FireAndForget();
            _channelsManager.StopReadingAsync().FireAndForget();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedCoreAsync" />
        protected override Task WaitUntilConsumingStoppedCoreAsync() =>
            Task.WhenAll(
                WaitUntilChannelsManagerStopsAsync(),
                WaitUntilConsumeLoopHandlerStopsAsync());

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.CommitCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        protected override Task CommitCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
        {
            var lastOffsets = brokerMessageIdentifiers
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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.RollbackCoreAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        [SuppressMessage("", "VSTHRD110", Justification = "stopping tasks awaited in Restart method")]
        protected override Task RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
        {
            var latestTopicPartitionOffsets =
                brokerMessageIdentifiers
                    .GroupBy(offset => offset.Key)
                    .Select(
                        offsetsGroup => offsetsGroup
                            .OrderBy(offset => offset.Offset)
                            .First()
                            .AsTopicPartitionOffset())
                    .ToList();

            if (IsConsuming)
            {
                _confluentConsumer?.Pause(
                    latestTopicPartitionOffsets.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition));
            }

            var channelsManagerStoppingTasks = new List<Task?>(latestTopicPartitionOffsets.Count);

            foreach (var topicPartitionOffset in latestTopicPartitionOffsets)
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

        /// <inheritdoc cref="Consumer.Dispose(bool)"/>
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

                var topicPartitions = latestTopicPartitionOffsets.Select(offset => offset.TopicPartition);

                foreach (var topicPartition in topicPartitions)
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

            if (Endpoint.TopicPartitionsResolver != null && Endpoint.TopicPartitions == null)
            {
                using var adminClient = ServiceProvider
                    .GetRequiredService<IConfluentAdminClientBuilder>()
                    .Build(Endpoint.Configuration.GetConfluentConfig());

                var availablePartitions = Endpoint.Names
                    .SelectMany(
                        topicName =>
                            adminClient.GetMetadata(topicName, TimeSpan.FromMinutes(5))
                                .Topics[0]
                                .Partitions
                                .Select(metadata => new TopicPartition(topicName, metadata.PartitionId)))
                    .ToList();

                Endpoint.TopicPartitions = Endpoint.TopicPartitionsResolver(availablePartitions)
                    .AsReadOnlyCollection();
            }

            if (Endpoint.TopicPartitions != null)
            {
                _confluentConsumer.Assign(Endpoint.TopicPartitions);

                Endpoint.TopicPartitions.ForEach(
                    topicPartitionOffset => _logger.LogPartitionManuallyAssigned(topicPartitionOffset, this));

                SetReadyStatus();
            }
            else
            {
                _confluentConsumer.Subscribe(Endpoint.Names);
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
            foreach (var offset in offsets)
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
            if (Endpoint.Configuration.IsAutoCommitEnabled)
                return;

            lock (_messagesSinceCommitLock)
            {
                if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach)
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
                var offsets = ConfluentConsumer.Commit();

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
                _callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                    handler => handler.OnOffsetsCommitted(
                        new CommittedOffsets(ex.Results, ex.Error),
                        this));

                throw;
            }
            catch (KafkaException ex)
            {
                _callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                    handler => handler.OnOffsetsCommitted(
                        new CommittedOffsets(null, ex.Error),
                        this));
            }
        }
    }
}
