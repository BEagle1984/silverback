using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Confluent.Kafka.Consumer<byte[], byte[]> _innerConsumer;
        private readonly ILogger<KafkaConsumer> _logger;

        private int _failCount = 0;

        public KafkaConsumer(IBroker broker, KafkaEndpoint endpoint, ILogger<KafkaConsumer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        public new KafkaEndpoint Endpoint => (KafkaEndpoint)base.Endpoint;

        internal void Connect()
        {
            if (_innerConsumer != null)
                return;

            _innerConsumer = new Confluent.Kafka.Consumer<byte[], byte[]>(Endpoint.Configuration,
                new ByteArrayDeserializer(),
                new ByteArrayDeserializer());

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

        internal void Disconnect()
        {
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
            _innerConsumer.Subscribe(Endpoint.TopicNames);

            _logger.LogTrace("Consuming topic '{topic}'...", Endpoint.Name);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!_innerConsumer.Consume(out var message, TimeSpan.FromMilliseconds(Endpoint.PollTimeout)))
                    continue;

                _logger.LogTrace("Consuming message: {topic} [{partition}] @{offset}", message.Topic, message.Partition, message.Offset);

                if (!await TryProcess(message))
                    break;
            }
        }

        private async Task<bool> TryProcess(Message<byte[], byte[]> message)
        {
            try
            {
                HandleMessage(message.Value);

                await CommitOffsetIfNeeded(message);

                _failCount = 0;
            }
            catch (Exception ex)
            {
                _failCount++;

                if (_failCount < Endpoint.ConsumerMaxTryCount)
                {
                    _logger.LogError(ex,
                        "Failed {failCount} time(s) to consume the message: {topic} [{partition}] @{offset}. The same message will be retried. See inner exception for details.",
                        _failCount, message.Topic, message.Partition, message.Offset);

                    _innerConsumer.Seek(message.TopicPartitionOffset);

                    await WaitDelay();
                }
                else
                {
                    _logger.LogCritical(ex,
                        "Failed {failCount} time(s) to consume the message: {topic} [{partition}] @{offset}. The consumer will be stopped. See inner exception for details.",
                        _failCount, message.Topic, message.Partition, message.Offset);

                    return false;
                }
            }

            return true;
        }

        private async Task CommitOffsetIfNeeded(Message<byte[], byte[]> message)
        {
            if (Endpoint.Configuration.IsAutocommitEnabled) return;
            if (message.Offset % Endpoint.CommitOffsetEach != 0) return;
            var committedOffsets = await _innerConsumer.CommitAsync(message);
            _logger.LogTrace("Committed offset: {offset}", committedOffsets);
        }

        private async Task WaitDelay()
        {
            var delay = Endpoint.ConsumerRetryDelay.Add(
                TimeSpan.FromMilliseconds(_failCount * Endpoint.ConsumerRetryDelayIncrement.Milliseconds));

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay);
        }
    }
}
