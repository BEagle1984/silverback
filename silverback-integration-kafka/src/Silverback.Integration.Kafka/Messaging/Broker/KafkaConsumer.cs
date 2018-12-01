// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint>, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<KafkaConsumer> _logger;

        private InnerConsumerWrapper[] _innerConsumerWrapper;

        private static readonly ConcurrentDictionary<Dictionary<string, object>, InnerConsumerWrapper> ConsumerWrappersCache =
            new ConcurrentDictionary<Dictionary<string, object>, InnerConsumerWrapper>(new KafkaConfigurationComparer());

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
                _innerConsumerWrapper =  new[] { ConsumerWrappersCache.GetOrAdd(Endpoint.Configuration, CreateInnerConsumerWrapper()) };
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
                consumerWrapper.Received += (message, retryCount) => OnMessageReceived(message, retryCount, consumerWrapper);
                consumerWrapper.StartConsuming();
            }
        }

        private InnerConsumerWrapper CreateInnerConsumerWrapper(int threadIndex = 1)
        {
            _logger.LogTrace("Creating Confluent.Kafka.Consumer...");

            Dictionary<string, object> configuration;

            if (Endpoint.ConsumerThreads > 1)
            {
                configuration = Endpoint.Configuration.ToDictionary(d => d.Key, d => d.Value);
                configuration["client.id"] = $"{configuration["client.id"]}[{threadIndex}]";
            }
            else
            {
                configuration = Endpoint.Configuration;
            }

            return new InnerConsumerWrapper(
                new Confluent.Kafka.Consumer<byte[], byte[]>(
                    configuration, new ByteArrayDeserializer(), new ByteArrayDeserializer()),
                _cancellationTokenSource.Token,
                _logger);
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

        private void OnMessageReceived(Message<byte[], byte[]> message, int retryCount, InnerConsumerWrapper innerConsumer)
        {
            if (!message.Topic.Equals(Endpoint.Name, StringComparison.InvariantCultureIgnoreCase))
                return;

            var result = TryHandleMessage(message, retryCount, innerConsumer);

            if (!result.IsSuccessful)
                HandleError(result.Action ?? ErrorAction.StopConsuming, message, retryCount, innerConsumer);
        }

        private MessageHandlerResult TryHandleMessage(Message<byte[], byte[]> message, int retryCount, InnerConsumerWrapper innerConsumer)
        {
            try
            {
                var result = HandleMessage(message.Value, retryCount);

                if (result.IsSuccessful)
                    CommitOffsetIfNeeded(message, innerConsumer);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Fatal error occurred consuming the message: {topic} [{partition}] @{offset}. " +
                    "The consumer will be stopped. See inner exception for details.",
                    message.Topic, message.Partition, message.Offset);

                return MessageHandlerResult.Error(ErrorAction.StopConsuming);
            }
        }

        private void CommitOffsetIfNeeded(Message<byte[], byte[]> message, InnerConsumerWrapper innerConsumer)
        {
            if (Endpoint.Configuration.IsAutocommitEnabled) return;
            if (message.Offset % Endpoint.CommitOffsetEach != 0) return;
            innerConsumer.Commit(message).Wait();
        }

        private void HandleError(ErrorAction action, Message<byte[], byte[]> message, int retryCount, InnerConsumerWrapper innerConsumer)
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
                    innerConsumer.Seek(message.TopicPartitionOffset);

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
                retryCount, message.Topic, message.Partition, message.Offset);
        }
    }
}
