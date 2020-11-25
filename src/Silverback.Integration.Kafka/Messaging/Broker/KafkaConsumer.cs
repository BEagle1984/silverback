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
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.KafkaEvents;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint, KafkaOffset>
    {
        private static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(5); // TODO: Could be configurable

        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(30); // TODO: Should be configurable

        private readonly IConfluentConsumerBuilder _confluentConsumerBuilder;

        private readonly ISilverbackIntegrationLogger<KafkaConsumer> _logger;

        private readonly object _messagesSinceCommitLock = new();

        private readonly object _channelsLock = new();

        private IKafkaMessageSerializer? _serializer;

        private ChannelsManager? _channelsManager;

        private ConsumeLoopHandler? _consumeLoopHandler;

        private int _messagesSinceCommit;

        private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

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
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public KafkaConsumer(
            KafkaBroker broker,
            KafkaConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<KafkaConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _logger = Check.NotNull(logger, nameof(logger));

            _confluentConsumerBuilder = serviceProvider.GetRequiredService<IConfluentConsumerBuilder>();
            _confluentConsumerBuilder.SetConfig(endpoint.Configuration.ConfluentConfig);
            _confluentConsumerBuilder.SetEventsHandlers(this, logger);
        }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId => _confluentConsumer?.MemberId ?? string.Empty;

        internal void OnPartitionsAssigned(IReadOnlyList<TopicPartition> partitions)
        {
            if (IsDisconnecting)
                return;

            lock (_channelsLock)
            {
                InitConsumeLoopHandlerAndChannelsManager(partitions);

                if (!IsConsuming)
                    return;

                _channelsManager?.StartReading();
                _consumeLoopHandler?.Start();
            }
        }

        internal void OnPartitionsRevoked()
        {
            lock (_channelsLock)
            {
                StopCore();
                AsyncHelper.RunSynchronously(() => WaitUntilConsumingStoppedAsync(CancellationToken.None));

                if (!Endpoint.Configuration.IsAutoCommitEnabled)
                    CommitOffsets();
            }

            SequenceStores.ForEach(store => store.Dispose());
            SequenceStores.Clear();
        }

        internal async Task HandleMessageAsync(
            Message<byte[]?, byte[]?> message,
            TopicPartitionOffset topicPartitionOffset)
        {
            Dictionary<string, string> logData = new();

            var offset = new KafkaOffset(topicPartitionOffset);
            logData["offset"] = $"{offset.Partition}@{offset.Offset}";

            var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

            if (message.Key != null)
            {
                string deserializedKafkaKey = _serializer!.DeserializeKey(
                    message.Key,
                    headers,
                    new MessageSerializationContext(Endpoint, topicPartitionOffset.Topic));

                headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, deserializedKafkaKey);
                headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deserializedKafkaKey);

                logData["kafkaKey"] = deserializedKafkaKey;
            }

            headers.AddOrReplace(KafkaMessageHeaders.TimestampKey, message.Timestamp.UtcDateTime.ToString("O"));

            await HandleMessageAsync(
                    message.Value,
                    headers,
                    topicPartitionOffset.Topic,
                    offset,
                    logData)
                .ConfigureAwait(false);
        }

        internal bool AutoRecoveryIfEnabled(KafkaException ex, CancellationToken cancellationToken)
        {
            if (!Endpoint.Configuration.EnableAutoRecovery)
            {
                const string errorMessage = "KafkaException occurred.. The consumer will be stopped. " +
                                            "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                            "(EnableAutoRecovery=true in the endpoint configuration). " +
                                            "(consumerId: {consumerId}, topic(s): {topics})";

                _logger.LogCritical(
                    KafkaEventIds.KafkaExceptionNoAutoRecovery,
                    ex,
                    errorMessage,
                    Id,
                    Endpoint.Names);

                return false;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                _logger.LogWarning(
                    KafkaEventIds.KafkaExceptionAutoRecovery,
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (consumerId: {consumerId}, topic(s): {topics})",
                    Id,
                    Endpoint.Names);

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
            _serializer = Endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(Endpoint.Serializer);

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

        /// <inheritdoc cref="Consumer.StartCore" />
        protected override void StartCore()
        {
            lock (_channelsLock)
            {
                // The consume loop must start immediately because the Confluent consumer connects for real only when
                // Consume is called
                _consumeLoopHandler ??= new ConsumeLoopHandler(this, _confluentConsumer!, _channelsManager, _logger);

                _consumeLoopHandler.Start();
                _channelsManager?.StartReading();

                // Set IsConsuming inside the lock because it is checked in OnPartitionsAssigned to decide to start the
                // channels manager
                IsConsuming = true;
            }
        }

        /// <inheritdoc cref="Consumer.StopCore" />
        protected override void StopCore()
        {
            lock (_channelsLock)
            {
                _consumeLoopHandler?.Stop();
                _channelsManager?.StopReading();
            }
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TIdentifier}.GetSequenceStore(IBrokerMessageIdentifier)" />
        protected override ISequenceStore GetSequenceStore(KafkaOffset brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return _channelsManager!.GetSequenceStore(brokerMessageIdentifier.AsTopicPartition());
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedAsync" />
        protected override async Task WaitUntilConsumingStoppedAsync(CancellationToken cancellationToken)
        {
            if (_consumeLoopHandler != null)
                await _consumeLoopHandler.Stopping.ConfigureAwait(false);

            if (_channelsManager != null)
                await _channelsManager.Stopping.ConfigureAwait(false);

            _consumeLoopHandler?.Dispose();
            _consumeLoopHandler = null;
            _channelsManager?.Dispose();
            _channelsManager = null;
        }

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
        protected override async Task RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> brokerMessageIdentifiers)
        {
            if (IsConsuming && _consumeLoopHandler != null)
                await _consumeLoopHandler.Stop().ConfigureAwait(false);

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

        private void Seek(TopicPartitionOffset topicPartitionOffset)
        {
            _channelsManager?.StopReading(topicPartitionOffset.TopicPartition);

            _confluentConsumer!.Seek(topicPartitionOffset);

            if (IsConsuming)
                _channelsManager?.StartReading(topicPartitionOffset.TopicPartition);
        }

        private void InitConfluentConsumer()
        {
            _confluentConsumer = _confluentConsumerBuilder.Build();
            Subscribe();
        }

        private void InitConsumeLoopHandlerAndChannelsManager(IReadOnlyList<TopicPartition> partitions)
        {
            _channelsManager ??= new ChannelsManager(partitions, this, SequenceStores, _logger);
            _consumeLoopHandler ??= new ConsumeLoopHandler(this, _confluentConsumer!, _channelsManager, _logger);
            _consumeLoopHandler.SetChannelsManager(_channelsManager);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void DisposeConfluentConsumer()
        {
            if (_confluentConsumer == null)
                return;

            var timeoutCancellationTokenSource = new CancellationTokenSource(CloseTimeout);

            try
            {
                // Workaround for Close getting stuck
                Task.Run(
                        () =>
                        {
                            _confluentConsumer?.Close();
                            _confluentConsumer?.Dispose();
                            _confluentConsumer = null;
                        },
                        timeoutCancellationTokenSource.Token)
                    .Wait(timeoutCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    KafkaEventIds.ConsumerDisconnectError,
                    ex,
                    "Error disconnecting consumer. (consumerId: {consumerId}, topic(s): {topics})",
                    Id,
                    Endpoint.Names);
            }
            finally
            {
                timeoutCancellationTokenSource.Dispose();
            }

            _confluentConsumer = null;
        }

        private void Subscribe() => _confluentConsumer!.Subscribe(Endpoint.Names);

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
                    _logger.LogCritical(
                        KafkaEventIds.ErrorRecoveringFromKafkaException,
                        ex,
                        "Failed to recover from consumer exception. Will retry in {SecondsUntilRetry} seconds. (consumerId: {consumerId}, topic(s): {topics})",
                        RecoveryDelay.TotalSeconds,
                        Id,
                        Endpoint.Names);

                    Task.Delay(RecoveryDelay, cancellationToken).Wait(cancellationToken);
                }
            }
        }

        private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            foreach (var offset in offsets)
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Storing offset {topic}[{partition}]@{offset}.",
                    offset.Topic,
                    offset.Partition.Value,
                    offset.Offset.Value);
                _confluentConsumer!.StoreOffset(offset);
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

            _confluentConsumer!.Commit();
        }

        private void CommitOffsets()
        {
            if (_messagesSinceCommit == 0)
                return;

            try
            {
                var offsets = _confluentConsumer!.Commit();

                Endpoint.Events.OffsetsCommittedHandler?.Invoke(
                    new CommittedOffsets(
                        offsets.Select(
                                offset =>
                                    new TopicPartitionOffsetError(
                                        offset,
                                        new Error(ErrorCode.NoError)))
                            .ToList(),
                        new Error(ErrorCode.NoError)),
                    this);
            }
            catch (TopicPartitionOffsetException ex)
            {
                Endpoint.Events.OffsetsCommittedHandler?.Invoke(
                    new CommittedOffsets(ex.Results, ex.Error),
                    this);

                throw;
            }
            catch (KafkaException ex)
            {
                Endpoint.Events.OffsetsCommittedHandler?.Invoke(
                    new CommittedOffsets(null, ex.Error),
                    this);
            }
        }
    }
}
