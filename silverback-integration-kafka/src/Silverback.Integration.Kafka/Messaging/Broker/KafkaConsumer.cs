// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint>, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<KafkaConsumer> _logger;

        private InnerConsumerWrapper[] _innerConsumerWrapper;

        private static readonly ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper> ConsumerWrappersCache =
            new ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper>(new KafkaClientConfigComparer());

        public KafkaConsumer(KafkaBroker broker, KafkaConsumerEndpoint endpoint, ILogger<KafkaConsumer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        internal void Connect()
        {
            if (_innerConsumerWrapper != null)
                return;

            if (Endpoint.ReuseConsumer && Endpoint.ConsumerThreads > 1)
                throw new SilverbackException("Invalid endpoint configuration. It is not allowed to set ReuseConsumer to true with multiple threads.");

            if (Endpoint.ReuseConsumer)
            {
                _innerConsumerWrapper = new[] { ConsumerWrappersCache.GetOrAdd(Endpoint.Configuration, CreateInnerConsumerWrapper()) };
            }
            else
            {
                _innerConsumerWrapper =
                    Enumerable.Range(1, Endpoint.ConsumerThreads)
                        .Select(CreateInnerConsumerWrapper)
                        .ToArray();
            }

            foreach (var consumerWrapper in _innerConsumerWrapper)
            {
                consumerWrapper.Subscribe(Endpoint);
                consumerWrapper.Received += (message, tpo, retryCount) => OnMessageReceived(message, tpo, retryCount, consumerWrapper);
                consumerWrapper.StartConsuming();
            }
        }

        internal void Disconnect()
        {
            if (_innerConsumerWrapper == null)
                return;

            // Remove from cache but take in account that it may have been removed already by another 
            // consumer sharing the same intance.
            ConsumerWrappersCache.TryRemove(Endpoint.Configuration, out var _);

            _cancellationTokenSource.Cancel();

            foreach (var consumerWrapper in _innerConsumerWrapper)
            {
                consumerWrapper.Dispose();
            }

            _innerConsumerWrapper = null;
        }

        public void Dispose()
        {
            Disconnect();
        }

        private InnerConsumerWrapper CreateInnerConsumerWrapper(int threadIndex = 1)
        {
            _logger.LogTrace("Creating Confluent.Kafka.Consumer...");

            var configuration = GetCurrentThreadConfiguration(threadIndex);

            return new InnerConsumerWrapper(
                new Confluent.Kafka.Consumer<byte[], byte[]>(configuration),
                _cancellationTokenSource.Token,
                _logger);
        }

        private IEnumerable<KeyValuePair<string, string>> GetCurrentThreadConfiguration(int threadIndex)
        {
            IEnumerable<KeyValuePair<string, string>> configuration;

            if (Endpoint.ConsumerThreads > 1)
            {
                var dict = Endpoint.Configuration.ToDictionary(d => d.Key, d => d.Value);
                dict["client.id"] = $"{dict["client.id"]}[{threadIndex}]";
                configuration = dict;
            }
            else
            {
                configuration = Endpoint.Configuration;
            }

            return configuration;
        }

        private void OnMessageReceived(Confluent.Kafka.Message<byte[], byte[]> message,
            Confluent.Kafka.TopicPartitionOffset tpo,
            int retryCount, InnerConsumerWrapper innerConsumer)
        {
            if (!tpo.Topic.Equals(Endpoint.Name, StringComparison.InvariantCultureIgnoreCase))
                return;

            var result = TryHandleMessage(message, tpo, retryCount, innerConsumer);

            if (!result.IsSuccessful)
                HandleError(result.Action ?? ErrorAction.StopConsuming, tpo, retryCount, innerConsumer);
        }

        private MessageHandlerResult TryHandleMessage(Confluent.Kafka.Message<byte[], byte[]> message,
            Confluent.Kafka.TopicPartitionOffset tpo,
            int retryCount, InnerConsumerWrapper innerConsumer)
        {
            try
            {
                var result = HandleMessage(message.Value, retryCount);

                if (result.IsSuccessful)
                    CommitOffsetIfNeeded(tpo, innerConsumer);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Fatal error occurred consuming the message: {topic} [{partition}] @{offset}. " +
                    "The consumer will be stopped. See inner exception for details.",
                    tpo.Topic, tpo.Partition, tpo.Offset);

                return MessageHandlerResult.Error(ErrorAction.StopConsuming);
            }
        }

        private void CommitOffsetIfNeeded(Confluent.Kafka.TopicPartitionOffset tpo, InnerConsumerWrapper innerConsumer)
        {
            if (Endpoint.Configuration.EnableAutoCommit ?? true) return;
            if (tpo.Offset % Endpoint.CommitOffsetEach != 0) return;
            innerConsumer.Commit(tpo);
        }

        private void HandleError(ErrorAction action, Confluent.Kafka.TopicPartitionOffset tpo, int retryCount, InnerConsumerWrapper innerConsumer)
        {
            string actionDescription = null;
            switch (action)
            {
                case ErrorAction.SkipMessage:
                    actionDescription = "This message will be skipped.";
                    break;
                case ErrorAction.RetryMessage:
                    actionDescription = "This message will be retried.";

                    // Revert offset to consume the same message again
                    innerConsumer.Seek(tpo);

                    break;
                case ErrorAction.StopConsuming:
                    actionDescription = "The consumer will be stopped.";
                    _cancellationTokenSource.Cancel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            _logger.LogTrace(
                "Error occurred consuming message: {topic} [{partition}] @{offset}. (retry={retryCount})" +
                actionDescription,
                tpo.Topic, tpo.Partition, tpo.Offset, retryCount);
        }
    }
}
