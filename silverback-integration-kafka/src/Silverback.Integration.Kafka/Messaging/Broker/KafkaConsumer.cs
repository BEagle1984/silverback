using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ILogger<KafkaConsumer> _logger;

        private Confluent.Kafka.Consumer<byte[], byte[]> _innerConsumer;

        private static readonly ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Consumer<byte[], byte[]>> ConsumersCache =
            new ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Consumer<byte[], byte[]>>(new ConfigurationComparer());

        public KafkaConsumer(IBroker broker, KafkaEndpoint endpoint, ILogger<KafkaConsumer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        public new KafkaEndpoint Endpoint => (KafkaEndpoint)base.Endpoint;

        internal void Connect()
        {
            if (_innerConsumer != null)
                return;

            _innerConsumer = ConsumersCache.GetOrAdd(Endpoint.Configuration, CreateInnerConsumer());

            if (!_innerConsumer.Subscription.Contains(Endpoint.Name))
                _innerConsumer.Subscribe(Endpoint.Name);

            _innerConsumer.OnPartitionsAssigned += (_, partitions) =>
            {
                _logger.LogTrace("Assigned partitions '{partitions}' to member with id '{memberId}'", string.Join(", ", partitions), _innerConsumer.MemberId);
                _innerConsumer.Assign(partitions);
            };

            _innerConsumer.OnPartitionsRevoked += (_, partitions) =>
            {
                _logger.LogTrace("Revoked '{partitions}' from member with id '{memberId}'", string.Join(", ", partitions), _innerConsumer.MemberId);
                _innerConsumer.Unassign();
            };

            Task.Run(Consume, _cancellationTokenSource.Token);
        }

        private Confluent.Kafka.Consumer<byte[], byte[]> CreateInnerConsumer() =>
            new Confluent.Kafka.Consumer<byte[], byte[]>(Endpoint.Configuration, new ByteArrayDeserializer(), new ByteArrayDeserializer());

        internal void Disconnect()
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!ConsumersCache.TryRemove(Endpoint.Configuration, out var _))
                return;

            _cancellationTokenSource.Cancel();

            _innerConsumer?.Unassign();
            _innerConsumer?.Unsubscribe();
            _innerConsumer?.Dispose();

            _innerConsumer = null;

        }

        public void Dispose()
        {
            Disconnect();
        }

        private async Task Consume()
        {
            _innerConsumer.Subscribe(Endpoint.Name);

            _logger.LogTrace("Consuming topic '{topic}'...", Endpoint.Name);

            var retryCount = 0;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!_innerConsumer.Consume(out var message, TimeSpan.FromMilliseconds(Endpoint.PollTimeout)))
                    continue;

                _logger.LogTrace("Consuming message: {topic} [{partition}] @{offset}", message.Topic, message.Partition, message.Offset);

                var result = await TryProcess(message, retryCount);

                if (!result.IsSuccessful)
                    retryCount = HandleError(result.Action ?? ErrorAction.StopConsuming, message, retryCount);
            }
        }

        private async Task<MessageHandlerResult> TryProcess(Message<byte[], byte[]> message, int retryCount)
        {
            try
            {
                var result = HandleMessage(message.Value, retryCount);

                if (result.IsSuccessful)
                    await CommitOffsetIfNeeded(message);

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


        private async Task CommitOffsetIfNeeded(Message<byte[], byte[]> message)
        {
            if (Endpoint.Configuration.IsAutocommitEnabled) return;
            if (message.Offset % Endpoint.CommitOffsetEach != 0) return;
            var committedOffsets = await _innerConsumer.CommitAsync(message);
            _logger.LogTrace("Committed offset: {offset}", committedOffsets);
        }

        private int HandleError(ErrorAction action, Message<byte[], byte[]> message, int retryCount)
        {
            string actionDescription = null;
            switch (action)
            {
                case ErrorAction.SkipMessage:
                    actionDescription = "This message will be skipped.";
                    retryCount = 0;
                    break;
                case ErrorAction.RetryMessage:
                    actionDescription = "This message will be retried.";
                    retryCount++;

                    // Revert offset to consume the same message again
                    _innerConsumer.Seek(message.TopicPartitionOffset);

                    break;
                case ErrorAction.StopConsuming:
                    actionDescription = "The consumer will be stopped.";
                    _cancellationTokenSource.Cancel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            _logger.LogTrace(
                "Error occurred consuming message (retry={retryCount}): {topic} [{partition}] @{offset}. " +
                actionDescription,
                retryCount, message.Topic, message.Partition, message.Offset);

            return retryCount;
        }
    }
}
