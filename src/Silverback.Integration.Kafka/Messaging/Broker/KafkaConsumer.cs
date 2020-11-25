// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
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
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
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

        private bool _isConsuming;

        private bool _hasConsumedAtLeastOnce;

        private int _messagesSinceCommit;

        private CancellationTokenSource? _cancellationTokenSource;

        private IConsumer<byte[]?, byte[]?>? _innerConsumer;

        private Channel<ConsumeResult<byte[]?, byte[]?>>[]? _channels;

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
            _cancellationTokenSource = new CancellationTokenSource();

            lock (_channelsLock)
            {
                _channels = Endpoint.ProcessPartitionsIndependently
                    ? new Channel<ConsumeResult<byte[]?, byte[]?>>[partitions.Count]
                    : new Channel<ConsumeResult<byte[]?, byte[]?>>[1];
            }

            if (Endpoint.ProcessPartitionsIndependently)
            {
                partitions.ForEach((_, i) => InitChannelReader(i));
            }
            else
            {
                InitChannelReader(0);
            }

            Task.Factory.StartNew(
                Consume,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        internal void OnPartitionsRevoked()
        {
            _cancellationTokenSource?.Cancel();
            WaitUntilConsumingStopped(CancellationToken.None);

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets();

            SequenceStores.ForEach(store => store.Dispose());
            SequenceStores.Clear();

            lock (_channelsLock)
            {
                _channels = null;
            }
        }

        /// <inheritdoc cref="Consumer.ConnectCore" />
        protected override void ConnectCore()
        {
            _serializer = Endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(Endpoint.Serializer);

            InitInnerConsumer();
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.GetSequenceStore(Silverback.Messaging.Broker.IOffset)" />
        protected override ISequenceStore GetSequenceStore(KafkaOffset offset)
        {
            Check.NotNull(offset, nameof(offset));

            if (_innerConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            return Endpoint.ProcessPartitionsIndependently
                ? SequenceStores[GetPartitionAssignmentIndex(offset.AsTopicPartition())]
                : SequenceStores[0];
        }

        /// <inheritdoc cref="Consumer.StopConsuming" />
        protected override void StopConsuming() => _cancellationTokenSource?.Cancel();

        /// <param name="cancellationToken"></param>
        /// <inheritdoc cref="Consumer.WaitUntilConsumingStopped" />
        protected override void WaitUntilConsumingStopped(CancellationToken cancellationToken)
        {
            while (_isConsuming && !cancellationToken.IsCancellationRequested)
            {
                AsyncHelper.RunSynchronously(() => Task.Delay(100, cancellationToken));
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <inheritdoc cref="Consumer.DisconnectCore" />
        protected override void DisconnectCore()
        {
            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets();

            DisposeInnerConsumer();
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.CommitCoreAsync" />
        protected override Task CommitCoreAsync(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_innerConsumer == null)
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
        protected override Task RollbackCoreAsync(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            offsets
                .GroupBy(offset => offset.Key)
                .Select(
                    offsetsGroup => offsetsGroup
                        .OrderBy(offset => offset.Offset)
                        .First()
                        .AsTopicPartitionOffset())
                .ForEach(
                    topicPartitionOffset =>
                    {
                        var partitionIndex = GetPartitionAssignmentIndex(topicPartitionOffset.TopicPartition);
                        if (partitionIndex >= 0)
                        {
                            lock (_channelsLock)
                            {
                                if (_channels != null)
                                {
                                    var channelIndex = GetChannelIndex(partitionIndex);

                                    // The new channel must be set before completing the old one to allow the process loop
                                    // to continue
                                    var currentChannel = _channels[channelIndex];
                                    _channels[channelIndex] = CreateBoundedChannel();
                                    currentChannel.Writer.Complete(new OperationCanceledException());
                                }
                            }

                            _innerConsumer.Seek(topicPartitionOffset);
                        }
                    });

            // Nothing to do here. With Kafka the uncommitted messages will be implicitly re-consumed.
            return Task.CompletedTask;
        }

        // TODO: Can test setting for backpressure limit?
        private Channel<ConsumeResult<byte[]?, byte[]?>> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumeResult<byte[]?, byte[]?>>(Endpoint.BackpressureLimit);

        private void InitChannelReader(int channelIndex)
        {
            if (_channels == null)
                throw new InvalidOperationException("The channels array is not initialized.");

            SequenceStores.Add(ServiceProvider.GetRequiredService<ISequenceStore>());
            _channels[channelIndex] = CreateBoundedChannel();
            Task.Run(() => ReadChannelAsync(channelIndex));
        }

        private int GetPartitionAssignmentIndex(TopicPartition topicPartition) =>
            _innerConsumer?.Assignment.IndexOf(topicPartition) ?? -1;

        private int GetChannelIndex(int partitionIndex) => Endpoint.ProcessPartitionsIndependently ? partitionIndex : 0;

        private void InitInnerConsumer()
        {
            _innerConsumer = _confluentConsumerBuilder.Build();
            Subscribe();
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void DisposeInnerConsumer()
        {
            if (_innerConsumer == null)
                return;

            var timeoutCancellationTokenSource = new CancellationTokenSource(CloseTimeout);

            try
            {
                // Workaround for Close getting stuck
                Task.Run(
                        () =>
                        {
                            _innerConsumer?.Close();
                            _innerConsumer?.Dispose();
                            _innerConsumer = null;
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

            _innerConsumer = null;
        }

        private void Subscribe()
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _innerConsumer.Subscribe(Endpoint.Names);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void Consume()
        {
            _isConsuming = true;

            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            while (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _innerConsumer.Consume(_cancellationTokenSource.Token);

                    if (consumeResult == null)
                        continue;

                    _hasConsumedAtLeastOnce = true;
                    _logger.LogDebug(
                        KafkaEventIds.ConsumingMessage,
                        "Consuming message: {topic} {partition} @{offset}.",
                        consumeResult.Topic,
                        consumeResult.Partition,
                        consumeResult.Offset);

                    var partitionIndex = GetPartitionAssignmentIndex(consumeResult.TopicPartition);
                    var channelIndex = GetChannelIndex(partitionIndex);

                    // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
                    // synchronously to stay within a single long-running thread.
                    AsyncHelper.RunSynchronously(
                        () => _channels![channelIndex].Writer.WriteAsync(
                            consumeResult,
                            _cancellationTokenSource.Token));
                }
                catch (OperationCanceledException)
                {
                    if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                        _logger.LogTrace(KafkaEventIds.ConsumingCanceled, "Consuming canceled.");
                }
                catch (KafkaException ex)
                {
                    if (!AutoRecoveryIfEnabled(ex))
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(
                        IntegrationEventIds.ConsumerFatalError,
                        ex,
                        "Fatal error occurred while consuming. The consumer will be stopped.");
                    break;
                }
            }

            _isConsuming = false;

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                Disconnect();
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync(int index)
        {
            try
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Starting channel {channelIndex} processing loop...",
                    index);

                if (_channels == null)
                    throw new InvalidOperationException("Channels not initialized.");

                while (true)
                {
                    var channelReader = _channels[index].Reader;

                    await ReadChannelOnceAsync(channelReader).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Exiting channel {channelIndex} processing loop (operation canceled).",
                    index);
            }
            catch (Exception ex)
            {
                // TODO: Prevent duplicate log (FatalExceptionLoggerConsumerBehavior)
                _logger.LogCritical(
                    IntegrationEventIds.ConsumerFatalError,
                    ex,
                    "Fatal error occurred processing the consumed message. The consumer will be stopped.");

                _cancellationTokenSource?.Cancel();
                Disconnect();
            }

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Exited channel {channelIndex) processing loop.",
                index);
        }

        private async Task ReadChannelOnceAsync(ChannelReader<ConsumeResult<byte[]?, byte[]?>> channelReader)
        {
            if (_cancellationTokenSource == null)
                throw new OperationCanceledException();

            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            var consumeResult = await channelReader.ReadAsync(_cancellationTokenSource.Token)
                .ConfigureAwait(false);

            _cancellationTokenSource?.Token.ThrowIfCancellationRequested();

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogInformation(
                    KafkaEventIds.EndOfPartition,
                    "Partition EOF reached: {topic} {partition} @{offset}.",
                    consumeResult.Topic,
                    consumeResult.Partition,
                    consumeResult.Offset);
                return;
            }

            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!Endpoint.Names.Any(
                endpointName =>
                    consumeResult.Topic.Equals(endpointName, StringComparison.OrdinalIgnoreCase)))
                return;

            await HandleMessageAsync(consumeResult.Message, consumeResult.TopicPartitionOffset)
                .ConfigureAwait(false);
        }

        private async Task HandleMessageAsync(
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

        private bool AutoRecoveryIfEnabled(KafkaException ex)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogWarning(
                    KafkaEventIds.KafkaExceptionAutoRecovery,
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (topic(s): {topics})",
                    (object)Endpoint.Names);

                ResetInnerConsumer();
            }
            else
            {
                const string errorMessage = "Fatal error occurred consuming a message. The consumer will be stopped. " +
                                            "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                            "(EnableAutoRecovery=true in the endpoint configuration). (topic(s): {topics})";

                _logger.LogCritical(
                    KafkaEventIds.KafkaExceptionNoAutoRecovery,
                    ex,
                    errorMessage,
                    (object)Endpoint.Names);
            }

            return Endpoint.Configuration.EnableAutoRecovery;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void ResetInnerConsumer()
        {
            while (true)
            {
                try
                {
                    DisposeInnerConsumer();
                    InitInnerConsumer();

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(
                        KafkaEventIds.ErrorRecoveringFromKafkaException,
                        ex,
                        "Failed to recover from consumer exception. Will retry in {SecondsUntilRetry} seconds.",
                        RecoveryDelay.TotalSeconds);

                    Task.Delay(RecoveryDelay, _cancellationTokenSource?.Token ?? CancellationToken.None).Wait();
                }
            }
        }

        private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            foreach (var offset in offsets)
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Storing offset {partition} @{offset}.",
                    offset.Partition,
                    offset.Offset.Value);
                _innerConsumer.StoreOffset(offset);
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

            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _innerConsumer.Commit();
        }

        private void CommitOffsets()
        {
            if (!_hasConsumedAtLeastOnce)
                return;

            try
            {
                if (_innerConsumer == null)
                    throw new InvalidOperationException("The underlying consumer is not initialized.");

                var offsets = _innerConsumer.Commit();
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
