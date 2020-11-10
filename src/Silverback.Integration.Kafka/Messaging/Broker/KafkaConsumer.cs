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

        private readonly KafkaEventsHandler _kafkaEventsHandler;

        private readonly ISilverbackIntegrationLogger<KafkaConsumer> _logger;

        private readonly object _messagesSinceCommitLock = new object();

        private readonly object _channelsLock = new object();

        private IKafkaMessageSerializer? _serializer;

        private ChannelsManager? _channelsManager;

        private ConsumeLoopHandler? _consumeLoopHandler;

        private int _messagesSinceCommit;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
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

            _confluentConsumerBuilder = serviceProvider.GetRequiredService<IConfluentConsumerBuilder>();
            _confluentConsumerBuilder.SetConfig(endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler = serviceProvider.GetRequiredService<KafkaEventsHandler>();
            _kafkaEventsHandler.SetConsumerEventsHandlers(this, _confluentConsumerBuilder);

            _logger = Check.NotNull(logger, nameof(logger));
        }

        internal void OnPartitionsAssigned(List<TopicPartition> partitions)
        {
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

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

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        internal async Task HandleMessageAsync(
            Message<byte[]?, byte[]?> message,
            TopicPartitionOffset topicPartitionOffset)
        {
            if (_serializer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            Dictionary<string, string> logData = new Dictionary<string, string>();

            var offset = new KafkaOffset(topicPartitionOffset);
            logData["offset"] = $"{offset.Partition}@{offset.Offset}";

            var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

            if (message.Key != null)
            {
                string deserializedKafkaKey = _serializer.DeserializeKey(
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
                const string errorMessage = "Fatal error occurred consuming a message. The consumer will be stopped. " +
                                            "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                            "(EnableAutoRecovery=true in the endpoint configuration). (topic(s): {topics})";

                _logger.LogCritical(
                    KafkaEventIds.KafkaExceptionNoAutoRecovery,
                    ex,
                    errorMessage,
                    (object)Endpoint.Names);

                return false;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                _logger.LogWarning(
                    KafkaEventIds.KafkaExceptionAutoRecovery,
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (topic(s): {topics})",
                    (object)Endpoint.Names);

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
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.GetSequenceStore(Silverback.Messaging.Broker.IOffset)" />
        protected override ISequenceStore GetSequenceStore(KafkaOffset offset)
        {
            Check.NotNull(offset, nameof(offset));

            if (_channelsManager == null)
                throw new InvalidOperationException("The ChannelsManager is not initialized.");

            return _channelsManager.GetSequenceStore(offset.AsTopicPartition());
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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.CommitCoreAsync" />
        protected override Task CommitCoreAsync(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            var lastOffsets = offsets
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

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.RollbackCoreAsync" />
        protected override async Task RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            if (IsConsuming && _consumeLoopHandler != null)
                await _consumeLoopHandler.Stop().ConfigureAwait(false);

            offsets
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
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _channelsManager?.StopReading(topicPartitionOffset.TopicPartition);

            _confluentConsumer.Seek(topicPartitionOffset);

            if (IsConsuming)
                _channelsManager?.StartReading(topicPartitionOffset.TopicPartition);
        }

        private void InitConfluentConsumer()
        {
            _confluentConsumer = _confluentConsumerBuilder.Build();
            Subscribe();
        }

        private void InitConsumeLoopHandlerAndChannelsManager(IList<TopicPartition> partitions)
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
                    "Error disconnecting consumer. (topic(s): {topics})",
                    (object)Endpoint.Names);
            }
            finally
            {
                timeoutCancellationTokenSource.Dispose();
            }

            _confluentConsumer = null;
        }

        private void Subscribe()
        {
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _confluentConsumer.Subscribe(Endpoint.Names);
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
                    _logger.LogCritical(
                        KafkaEventIds.ErrorRecoveringFromKafkaException,
                        ex,
                        "Failed to recover from consumer exception. Will retry in {SecondsUntilRetry} seconds.",
                        RecoveryDelay.TotalSeconds);

                    Task.Delay(RecoveryDelay, cancellationToken).Wait(cancellationToken);
                }
            }
        }

        private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_confluentConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            foreach (var offset in offsets)
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Storing offset {partition}@{offset}.",
                    offset.Partition.Value,
                    offset.Offset.Value);
                _confluentConsumer.StoreOffset(offset);
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

            if (_confluentConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _confluentConsumer.Commit();
        }

        private void CommitOffsets()
        {
            if (_messagesSinceCommit == 0)
                return;

            try
            {
                if (_confluentConsumer == null)
                    throw new InvalidOperationException("The underlying consumer is not initialized.");

                var offsets = _confluentConsumer.Commit();
                _kafkaEventsHandler.CreateScopeAndPublishEvent(
                    new KafkaOffsetsCommittedEvent(
                        offsets.Select(
                                offset =>
                                    new TopicPartitionOffsetError(
                                        offset,
                                        new Error(ErrorCode.NoError)))
                            .ToList()));
            }
            catch (TopicPartitionOffsetException ex)
            {
                _kafkaEventsHandler.CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(ex.Results, ex.Error));
                throw;
            }
            catch (KafkaException ex)
            {
                _kafkaEventsHandler.CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(null, ex.Error));
            }
        }
    }
}
