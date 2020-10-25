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

        private IKafkaMessageSerializer? _serializer;

        private bool _isConsuming;

        private bool _hasConsumedAtLeastOnce;

        private int _messagesSinceCommit;

        private CancellationTokenSource? _cancellationTokenSource;

        private IConsumer<byte[]?, byte[]?>? _innerConsumer;

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

        /// <inheritdoc cref="Consumer.ConnectCore" />
        protected override void ConnectCore()
        {
            _serializer = Endpoint.Serializer as IKafkaMessageSerializer ??
                          new DefaultKafkaMessageSerializer(Endpoint.Serializer);

            _cancellationTokenSource = new CancellationTokenSource();

            InitInnerConsumer();

            Task.Factory.StartNew(
                ConsumeAsync,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <inheritdoc cref="Consumer.DisconnectCore" />
        protected override void DisconnectCore()
        {
            StopConsumingAsync().Wait();

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
                        if (_innerConsumer.Assignment.Contains(topicPartitionOffset.TopicPartition))
                            _innerConsumer.Seek(topicPartitionOffset);
                    });

            // Nothing to do here. With Kafka the uncommitted messages will be implicitly re-consumed.
            return Task.CompletedTask;
        }

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
        private async Task ConsumeAsync()
        {
            _isConsuming = true;

            if (_cancellationTokenSource == null)
            {
                throw new InvalidOperationException(
                    "The cancellation token is null, probably because the consumer is not properly connected.");
            }

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await ReceiveMessageAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace(KafkaEventIds.ConsumingCanceled, "Consuming canceled.");
                }
                catch (KafkaException ex)
                {
                    if (!await AutoRecoveryIfEnabledAsync(ex).ConfigureAwait(false))
                        break;
                }
                catch
                {
                    /* Logged by the FatalExceptionLoggerConsumerBehavior */

                    break;
                }
            }

            _isConsuming = false;

            if (!_cancellationTokenSource.IsCancellationRequested)
                Disconnect();
        }

        private async Task ReceiveMessageAsync()
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
                    KafkaEventIds.EndOfPartition,
                    "Partition EOF reached: {topic} {partition} @{offset}.",
                    result.Topic,
                    result.Partition,
                    result.Offset);
                return;
            }

            _hasConsumedAtLeastOnce = true;
            _logger.LogDebug(
                KafkaEventIds.ConsumingMessage,
                "Consuming message: {topic} {partition} @{offset}.",
                result.Topic,
                result.Partition,
                result.Offset);

            await OnMessageReceivedAsync(result.Message, result.TopicPartitionOffset).ConfigureAwait(false);
        }

        private async Task OnMessageReceivedAsync(
            Message<byte[]?, byte[]?> message,
            TopicPartitionOffset topicPartitionOffset)
        {
            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!Endpoint.Names.Any(
                endpointName =>
                    topicPartitionOffset.Topic.Equals(endpointName, StringComparison.OrdinalIgnoreCase)))
                return;

            await TryHandleMessageAsync(message, topicPartitionOffset).ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessageAsync(Message<byte[]?, byte[]?> message, TopicPartitionOffset tpo)
        {
            if (_serializer == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _messagesSinceCommit++;

            Dictionary<string, string> logData = new Dictionary<string, string>();

            var offset = new KafkaOffset(tpo);
            logData["offset"] = $"{offset.Partition}@{offset.Offset}";

            var headers = new MessageHeaderCollection(message.Headers.ToSilverbackHeaders());

            if (message.Key != null)
            {
                string deserializedKafkaKey = _serializer.DeserializeKey(
                    message.Key,
                    headers,
                    new MessageSerializationContext(Endpoint, tpo.Topic));

                headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, deserializedKafkaKey);
                headers.AddIfNotExists(DefaultMessageHeaders.MessageId, deserializedKafkaKey);

                logData["kafkaKey"] = deserializedKafkaKey;
            }

            // TODO: Use Offset as message-id if still empty?

            headers.AddOrReplace(KafkaMessageHeaders.TimestampKey, message.Timestamp.UtcDateTime.ToString("O"));

            await HandleMessageAsync(
                    message.Value,
                    headers,
                    tpo.Topic,
                    offset,
                    logData)
                .ConfigureAwait(false);
        }

        private async Task<bool> AutoRecoveryIfEnabledAsync(KafkaException ex)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogWarning(
                    KafkaEventIds.KafkaExceptionAutoRecovery,
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (topic(s): {topics})",
                    (object)Endpoint.Names);

                await ResetInnerConsumerAsync().ConfigureAwait(false);
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
        private async Task ResetInnerConsumerAsync()
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

                    await Task.Delay(RecoveryDelay).ConfigureAwait(false);
                }
            }
        }

        private void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            offsets.ForEach(_innerConsumer.StoreOffset);
        }

        private void CommitOffsetsIfNeeded()
        {
            if (Endpoint.Configuration.IsAutoCommitEnabled)
                return;

            if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach)
                return;

            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _innerConsumer.Commit();

            _messagesSinceCommit = 0;
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

        private async Task StopConsumingAsync()
        {
            if (_innerConsumer == null)
                return;

            _cancellationTokenSource?.Cancel();

            // Wait until stopped for real before returning to avoid
            // exceptions when the process exits prematurely
            while (_isConsuming)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
