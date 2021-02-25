// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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
        private static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(5);

        private readonly IConfluentConsumerBuilder _confluentConsumerBuilder;

        private readonly IBrokerCallbacksInvoker _callbacksInvoker;

        private readonly IInboundLogger<KafkaConsumer> _logger;

        private readonly object _messagesSinceCommitLock = new();

        private readonly object _channelsLock = new();

        private readonly IKafkaMessageSerializer _serializer;

        private ConsumerChannelsManager? _channelsManager;

        private ConsumeLoopHandler? _consumeLoopHandler;

        private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

        private int _messagesSinceCommit;

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
        }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId => _confluentConsumer?.MemberId ?? string.Empty;

        internal IConsumer<byte[]?, byte[]?> ConfluentConsumer =>
            _confluentConsumer ?? throw new InvalidOperationException("ConfluentConsumer not set.");

        private ConsumerChannelsManager ChannelsManager =>
            _channelsManager ?? throw new InvalidOperationException("ChannelsManager not set.");

        internal void OnPartitionsAssigned(IReadOnlyList<TopicPartition> partitions)
        {
            if (IsDisconnecting)
                return;

            lock (_channelsLock)
            {
                IsConsuming = true;

                InitAndStartChannelsManager(partitions);
            }
        }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        internal void OnPartitionsRevoked()
        {
            StopConsumeLoopHandlerAndChannelsManager();

            AsyncHelper.RunSynchronously(WaitUntilChannelsManagerStopsAsync);

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets();

            AsyncHelper.RunSynchronously(
                () => SequenceStores.DisposeAllAsync(SequenceAbortReason.ConsumerAborted));
            SequenceStores.Clear();

            // The ConsumeLoopHandler needs to be immediately restarted because the partitions will be
            // reassigned only if Consume is called again.
            // Furthermore the ConsumeLoopHandler stopping Task cannot be awaited in the
            // OnPartitionsRevoked callback, before the partitions are revoked because the Consume method is
            // "frozen" during that operation and will never return, therefore the stopping Task would never
            // complete. Therefore, let's start an async Task to await it and restart the ChannelManager.
            Task.Run(RestartConsumeLoopHandlerAsync);
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

        internal bool AutoRecoveryIfEnabled(Exception ex, CancellationToken cancellationToken)
        {
            if (!Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogKafkaExceptionNoAutoRecovery(this, ex);
                return false;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                _logger.LogKafkaExceptionAutoRecovery(this, ex);

                ResetConfluentConsumer(cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        internal void CreateSequenceStores(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (SequenceStores.Count <= i)
                    SequenceStores.Add(ServiceProvider.GetRequiredService<ISequenceStore>());
            }
        }

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
            lock (_channelsLock)
            {
                // Set IsConsuming before calling the init methods, otherwise they don't start for real
                IsConsuming = true;

                // The consume loop must start immediately because the partitions assignment is received
                // only after Consume is called
                InitAndStartConsumeLoopHandler();

                // Start reading from the channels right away in case of static partitions assignment or if
                // the consumer is restarting and the partition assignment is set already
                if (Endpoint.TopicPartitions != null ||
                    _confluentConsumer?.Assignment != null && _confluentConsumer.Assignment.Count > 0)
                {
                    InitAndStartChannelsManager(ConfluentConsumer.Assignment);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            StopConsumeLoopHandlerAndChannelsManager();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.GetSequenceStore(IBrokerMessageIdentifier)" />
        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Sync start/stop only")]
        protected override ISequenceStore GetSequenceStore(KafkaOffset brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return ChannelsManager.GetSequenceStore(brokerMessageIdentifier.AsTopicPartition());
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
        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Sync start/stop only")]
        protected override async Task RollbackCoreAsync(
            IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
        {
            if (IsConsuming && _consumeLoopHandler != null)
                await _consumeLoopHandler.StopAsync().ConfigureAwait(false);

            brokerMessageIdentifiers
                .GroupBy(offset => offset.Key)
                .Select(
                    offsetsGroup => offsetsGroup
                        .OrderBy(offset => offset.Offset)
                        .First()
                        .AsTopicPartitionOffset())
                .ForEach(Seek);

            if (IsConsuming)
                _consumeLoopHandler?.Start();
        }

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Sync start/stop only")]
        private void Seek(TopicPartitionOffset topicPartitionOffset)
        {
            _channelsManager?.StopReading(topicPartitionOffset.TopicPartition);

            ConfluentConsumer.Seek(topicPartitionOffset);

            if (IsConsuming)
                _channelsManager?.StartReading(topicPartitionOffset.TopicPartition);
        }

        private void InitConfluentConsumer()
        {
            _confluentConsumer = _confluentConsumerBuilder.Build();

            if (Endpoint.TopicPartitions == null)
            {
                _confluentConsumer.Subscribe(Endpoint.Names);
            }
            else
            {
                _confluentConsumer.Assign(Endpoint.TopicPartitions);

                Endpoint.TopicPartitions.ForEach(
                    topicPartitionOffset => _logger.LogPartitionManuallyAssigned(topicPartitionOffset, this));
            }
        }

        private void InitAndStartConsumeLoopHandler()
        {
            _consumeLoopHandler ??= new ConsumeLoopHandler(this, _channelsManager, _logger);

            if (_channelsManager != null)
                _consumeLoopHandler.SetChannelsManager(_channelsManager);

            if (IsConsuming)
                _consumeLoopHandler.Start();
        }

        private void InitAndStartChannelsManager(IReadOnlyList<TopicPartition> partitions)
        {
            _channelsManager ??= new ConsumerChannelsManager(partitions, this, SequenceStores, _logger);

            _consumeLoopHandler?.SetChannelsManager(_channelsManager);

            if (IsConsuming)
                _channelsManager.StartReading();
        }

        private async Task RestartConsumeLoopHandlerAsync()
        {
            await WaitUntilConsumeLoopHandlerStopsAsync().ConfigureAwait(false);

            InitAndStartConsumeLoopHandler();
        }

        private void StopConsumeLoopHandlerAndChannelsManager()
        {
            lock (_channelsLock)
            {
                _consumeLoopHandler?.StopAsync();
                _channelsManager?.StopReading();
            }
        }

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Sync start/stop only")]
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

            _consumeLoopHandler?.Dispose();
            _consumeLoopHandler = null;
        }

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Sync start/stop only")]
        private async Task WaitUntilChannelsManagerStopsAsync()
        {
            if (_channelsManager == null)
            {
                _logger.LogConsumerLowLevelTrace(this, "ChannelsManager is null.");
                return;
            }

            _logger.LogConsumerLowLevelTrace(this, "Waiting until ChannelsManager stops...");
            await _channelsManager.Stopping.ConfigureAwait(false);
            _logger.LogConsumerLowLevelTrace(this, "ChannelsManager stopped.");

            _channelsManager?.Dispose();
            _channelsManager = null;

            _logger.LogConsumerLowLevelTrace(this, "ChannelsManager disposed.");
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
                _logger.LogConsumerDisconnectError(this, ex);
            }

            _confluentConsumer = null;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void ResetConfluentConsumer(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    DisposeConfluentConsumer();
                    InitConfluentConsumer();

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogErrorRecoveringFromKafkaException(RecoveryDelay, this, ex);

                    Task.Delay(RecoveryDelay, cancellationToken).Wait(cancellationToken);
                }
            }
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
