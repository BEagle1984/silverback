// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal sealed class KafkaInnerConsumerWrapper : IDisposable
    {
        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(5);

        private readonly KafkaEventsHandler _kafkaEventsHandler;

        private readonly ILogger _logger;

        private CancellationTokenSource? _cancellationTokenSource;

        private IConsumer<byte[], byte[]>? _innerConsumer;

        private List<object>? _deserializers;

        public KafkaInnerConsumerWrapper(
            KafkaConsumerEndpoint endpoint,
            KafkaEventsHandler kafkaEventsHandler,
            ILogger logger)
        {
            Endpoint = endpoint;
            _kafkaEventsHandler = kafkaEventsHandler;
            _logger = logger;
        }

        public event KafkaMessageReceivedHandler? Received;

        public KafkaConsumerEndpoint Endpoint { get; }

        public bool IsConsuming { get; private set; }

        public bool HasConsumedAtLeastOnce { get; private set; }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            _innerConsumer.Commit(offsets);
        }

        public void StoreOffset(IEnumerable<TopicPartitionOffset> offsets)
        {
            if (_innerConsumer == null)
                throw new InvalidOperationException("The underlying consumer is not initialized.");

            offsets.ForEach(_innerConsumer.StoreOffset);
        }

        public void CommitAll()
        {
            if (!HasConsumedAtLeastOnce)
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

        public void StartConsuming()
        {
            if (IsConsuming)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            InitInnerConsumer();

            Task.Run(Consume);
        }

        public void StopConsuming()
        {
            if (!IsConsuming)
                return;

            _cancellationTokenSource?.Cancel();

            // Wait until stopped for real before returning to avoid
            // exceptions when the process exits prematurely
            while (IsConsuming)
            {
                Thread.Sleep(100);
            }

            _cancellationTokenSource?.Dispose();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            StopConsuming();
            DisposeInnerConsumer();
            _cancellationTokenSource?.Dispose();
        }

        private void InitInnerConsumer()
        {
            var consumerBuilder =
                new ConsumerBuilder<byte[], byte[]>(Endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler.SetConsumerEventsHandlers(this, consumerBuilder);

            _innerConsumer = consumerBuilder.Build();
            Subscribe();
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
            IsConsuming = true;

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
                    _logger.LogTrace(
                        EventIds.KafkaInnerConsumerWrapperConsumingCanceled,
                        "Consuming canceled.");
                }
                catch (KafkaException ex)
                {
                    if (!AutoRecoveryIfEnabled(ex))
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(
                        EventIds.KafkaInnerConsumerWrapperFatalError,
                        ex,
                        "Fatal error occurred consuming a message. The consumer will be stopped.");
                    break;
                }
            }

            IsConsuming = false;
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
                    EventIds.KafkaInnerConsumerWrapperEndOfPartition,
                    "Partition EOF reached: {topic} {partition} @{offset}.",
                    result.Topic,
                    result.Partition,
                    result.Offset);
                return;
            }

            HasConsumedAtLeastOnce = true;
            _logger.LogDebug(
                EventIds.KafkaInnerConsumerWrapperConsumingMessage,
                "Consuming message: {topic} {partition} @{offset}.",
                result.Topic,
                result.Partition,
                result.Offset);

            if (Received != null)
                await Received.Invoke(result.Message, result.TopicPartitionOffset);
        }

        private bool AutoRecoveryIfEnabled(KafkaException ex)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogWarning(
                    EventIds.KafkaInnerConsumerWrapperKafkaException,
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
                    EventIds.KafkaInnerConsumerWrapperNoReconnectFatalError,
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
                        EventIds.KafkaInnerConsumerWrapperFaildToRecoverFromConsumerException,
                        ex,
                        "Failed to recover from consumer exception. Will retry in {SecondsUntilRetry} seconds.",
                        _recoveryDelay.TotalSeconds);

                    Thread.Sleep(_recoveryDelay);
                }
            }
        }

        private void DisposeInnerConsumer()
        {
            _innerConsumer?.Close();
            _innerConsumer?.Dispose();
            _innerConsumer = null;
            _deserializers?.OfType<IDisposable>().ForEach(obj => obj.Dispose());
            _deserializers = null;
        }
    }
}
