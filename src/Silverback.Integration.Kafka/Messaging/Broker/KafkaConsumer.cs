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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint, KafkaOffset>
    {
        private static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(5); // TODO: Could be configurable

        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(30); // TODO: Should be configurable

        private readonly ILogger<KafkaConsumer> _logger;

        private readonly KafkaEventsHandler _kafkaEventsHandler;

        private IKafkaMessageSerializer? _serializer;

        private bool _isConsuming;

        private bool _hasConsumedAtLeastOnce;

        private int _messagesSinceCommit;

        private CancellationTokenSource? _cancellationTokenSource;

        private IConsumer<byte[], byte[]>? _innerConsumer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="callback">
        ///     The delegate to be invoked when a message is received.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public KafkaConsumer(
            KafkaBroker broker,
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
            IServiceProvider serviceProvider1 = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            _kafkaEventsHandler = serviceProvider1.GetRequiredService<KafkaEventsHandler>();
        }

        /// <inheritdoc cref="Consumer.ConnectCore" />
        protected override void ConnectCore()
        {
            _serializer = Endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(Endpoint.Serializer);

            _cancellationTokenSource = new CancellationTokenSource();

            InitInnerConsumer();

            Task.Run(Consume);
        }

        /// <inheritdoc cref="Consumer.DisconnectCore" />
        protected override void DisconnectCore()
        {
            StopConsuming().Wait();

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitAll();

            DisposeInnerConsumer();
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.CommitCore" />
        protected override Task CommitCore(IReadOnlyCollection<KafkaOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            var lastOffsets = offsets
                .GroupBy(offset => offset.Key)
                .Select(
                    offsetsGroup => offsetsGroup
                        .OrderByDescending(offset => offset.Value)
                        .First()
                        .AsTopicPartitionOffset())
                .ToList();

            StoreOffset(
                lastOffsets
                    .Select(
                        topicPartitionOffset => new TopicPartitionOffset(
                            topicPartitionOffset.TopicPartition,
                            topicPartitionOffset.Offset + 1))
                    .ToArray());

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets(lastOffsets);

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}.RollbackCore" />
        protected override Task RollbackCore(IReadOnlyCollection<KafkaOffset> offsets)
        {
            // Nothing to do here. With Kafka the uncommitted messages will be implicitly re-consumed.
            return Task.CompletedTask;
        }

        private void InitInnerConsumer()
        {
            var consumerBuilder =
                new ConsumerBuilder<byte[], byte[]>(Endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler.SetConsumerEventsHandlers(this, consumerBuilder);

            _innerConsumer = consumerBuilder.Build();
            Subscribe();
        }

        private void DisposeInnerConsumer()
        {
            if (_innerConsumer == null)
                return;

            using var timeoutCancellationTokenSource = new CancellationTokenSource(CloseTimeout);

            try
            {
                // Workaround for Close getting stuck
                Task.Run(
                        () =>
                        {
                            _innerConsumer.Close();
                            _innerConsumer.Dispose();
                        },
                        timeoutCancellationTokenSource.Token)
                    .Wait(timeoutCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignored
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
        private async Task Consume()
        {
            _isConsuming = true;

            if (_cancellationTokenSource == null)
            {
                throw new InvalidOperationException(
                    "The cancellation token is null, probably because the underlying " +
                    "consumer is not initialized.");
            }

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await ReceiveMessage();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace(EventIds.KafkaConsumerConsumingCanceled, "Consuming canceled.");
                }
                catch (KafkaException ex)
                {
                    if (!await AutoRecoveryIfEnabled(ex))
                        break;
                }
                catch (Exception)
                {
                    /* Logged by the FatalExceptionLoggerConsumerBehavior */

                    break;
                }
            }

            _isConsuming = false;
            Disconnect();
        }

        private async Task ReceiveMessage()
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            if (_cancellationTokenSource == null)
            {
                throw new InvalidOperationException(
                    "The cancellation token is null, probably because the underlying " +
                    "consumer is not initialized.");
            }

            var result = _innerConsumer.Consume(_cancellationTokenSource.Token);

            if (result == null)
                return;

            if (result.IsPartitionEOF)
            {
                _logger.LogInformation(
                    EventIds.KafkaConsumerEndOfPartition,
                    "Partition EOF reached: {topic} {partition} @{offset}.",
                    result.Topic,
                    result.Partition,
                    result.Offset);
                return;
            }

            _hasConsumedAtLeastOnce = true;
            _logger.LogDebug(
                EventIds.KafkaConsumerConsumingMessage,
                "Consuming message: {topic} {partition} @{offset}.",
                result.Topic,
                result.Partition,
                result.Offset);

            await OnMessageReceived(result.Message, result.TopicPartitionOffset);
        }

        private async Task OnMessageReceived(
            Message<byte[], byte[]> message,
            TopicPartitionOffset topicPartitionOffset)
        {
            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!Endpoint.Names.Any(
                endpointName =>
                    topicPartitionOffset.Topic.Equals(endpointName, StringComparison.OrdinalIgnoreCase)))
                return;

            await TryHandleMessage(message, topicPartitionOffset);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessage(Message<byte[], byte[]> message, TopicPartitionOffset tpo)
        {
            if (_serializer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _messagesSinceCommit++;
            KafkaOffset? offset = new KafkaOffset(tpo);

            var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

            if (message.Key != null)
            {
                headers.AddOrReplace(
                    KafkaMessageHeaders.KafkaMessageKey,
                    _serializer.DeserializeKey(
                        message.Key,
                        headers,
                        new MessageSerializationContext(Endpoint, tpo.Topic)));
            }

            await HandleMessage(
                message.Value,
                headers,
                tpo.Topic,
                offset);
        }

        private async Task<bool> AutoRecoveryIfEnabled(KafkaException ex)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogWarning(
                    EventIds.KafkaConsumerKafkaException,
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (topic(s): {topics})",
                    (object)Endpoint.Names);

                await ResetInnerConsumer();
            }
            else
            {
                const string errorMessage = "Fatal error occurred consuming a message. The consumer will be stopped. " +
                                            "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                            "(EnableAutoRecovery=true in the endpoint configuration). (topic(s): {topics})";

                _logger.LogCritical(
                    EventIds.KafkaConsumerNoReconnectFatalError,
                    ex,
                    errorMessage,
                    (object)Endpoint.Names);
            }

            return Endpoint.Configuration.EnableAutoRecovery;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ResetInnerConsumer()
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
                        EventIds.KafkaConsumerFailedToRecoverFromConsumerException,
                        ex,
                        "Failed to recover from consumer exception. Will retry in {SecondsUntilRetry} seconds.",
                        RecoveryDelay.TotalSeconds);

                    await Task.Delay(RecoveryDelay);
                }
            }
        }

        private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            offsets.ForEach(_innerConsumer.StoreOffset);
        }

        private void CommitOffsets(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach)
                return;

            _innerConsumer.Commit(offsets);

            _messagesSinceCommit = 0;
        }

        private void CommitAll()
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

        private async Task StopConsuming()
        {
            if (_innerConsumer == null)
                return;

            _cancellationTokenSource?.Cancel();

            // Wait until stopped for real before returning to avoid
            // exceptions when the process exits prematurely
            while (_isConsuming)
            {
                await Task.Delay(100);
            }

            _cancellationTokenSource?.Dispose();
        }
    }
}
