// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal class KafkaInnerConsumerWrapper : IDisposable
    {
        private readonly KafkaEventsHandler _kafkaEventsHandler;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(5);

        private Confluent.Kafka.IConsumer<byte[], byte[]> _innerConsumer;
        private List<object> _deserializers;

        public KafkaInnerConsumerWrapper(
            KafkaConsumerEndpoint endpoint,
            KafkaEventsHandler kafkaEventsHandler,
            ILogger logger)
        {
            Endpoint = endpoint;
            _kafkaEventsHandler = kafkaEventsHandler;
            _logger = logger;
        }

        public event KafkaMessageReceivedHandler Received;

        public KafkaConsumerEndpoint Endpoint { get; }
        public bool IsConsuming { get; private set; }
        public bool HasConsumedAtLeastOnce { get; private set; }

        public void Commit(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets) =>
            _innerConsumer.Commit(offsets);

        public void StoreOffset(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets) =>
            offsets.ForEach(_innerConsumer.StoreOffset);

        public void CommitAll()
        {
            if (!HasConsumedAtLeastOnce) return;

            try
            {
                var offsets = _innerConsumer.Commit();
                _kafkaEventsHandler.CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(
                    offsets.Select(offset =>
                            new Confluent.Kafka.TopicPartitionOffsetError(
                                offset,
                                new Confluent.Kafka.Error(Confluent.Kafka.ErrorCode.NoError)))
                        .ToList()));
            }
            catch (Confluent.Kafka.TopicPartitionOffsetException ex)
            {
                _kafkaEventsHandler.CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(ex.Results, ex.Error));
                throw;
            }
            catch (Confluent.Kafka.KafkaException ex)
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

            _cancellationTokenSource.Cancel();

            // Wait until stopped for real before returning to avoid
            // exceptions when the process exits prematurely
            while (IsConsuming) Thread.Sleep(100);

            _cancellationTokenSource.Dispose();
        }

        private void InitInnerConsumer()
        {
            var consumerBuilder =
                new Confluent.Kafka.ConsumerBuilder<byte[], byte[]>(Endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler.SetConsumerEventsHandlers(this, consumerBuilder);

            _innerConsumer = consumerBuilder.Build();
            Subscribe();
        }

        private void Subscribe() => _innerConsumer.Subscribe(Endpoint.Names);

        private async Task Consume()
        {
            IsConsuming = true;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await ReceiveMessage();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Consuming canceled.");
                }
                catch (Confluent.Kafka.KafkaException ex)
                {
                    if (!AutoRecoveryIfEnabled(ex))
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Fatal error occurred consuming a message. The consumer will be stopped.");
                    break;
                }
            }

            IsConsuming = false;
        }

        private async Task ReceiveMessage()
        {
            var result = _innerConsumer.Consume(_cancellationTokenSource.Token);

            if (result == null)
                return;

            if (result.IsPartitionEOF)
            {
                _logger.LogInformation("Partition EOF reached: {topic} {partition} @{offset}.",
                    result.Topic, result.Partition, result.Offset);
                return;
            }

            HasConsumedAtLeastOnce = true;
            _logger.LogDebug("Consuming message: {topic} {partition} @{offset}.",
                result.Topic, result.Partition, result.Offset);

            if (Received != null)
                await Received.Invoke(result.Message, result.TopicPartitionOffset);
        }

        private bool AutoRecoveryIfEnabled(Confluent.Kafka.KafkaException ex)
        {
            if (Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogWarning(
                    ex,
                    "KafkaException occurred. The consumer will try to recover. (topic(s): {topics})",
                    (object) Endpoint.Names);

                ResetInnerConsumer();
            }
            else
            {
                _logger.LogCritical(
                    ex,
                    "Fatal error occurred consuming a message. The consumer will be stopped. " +
                    "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                    "(EnableAutoRecovery=true in the endpoint configuration). (topic(s): {topics})",
                    (object) Endpoint.Names);
            }

            return Endpoint.Configuration.EnableAutoRecovery;
        }

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
                    _logger.LogCritical(ex,
                        "Failed to recover from consumer exception. " +
                        $"Will retry in {_recoveryDelay.TotalSeconds} seconds.");

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

        public void Dispose()
        {
            StopConsuming();
            DisposeInnerConsumer();
            _cancellationTokenSource?.Dispose();
        }
    }
}