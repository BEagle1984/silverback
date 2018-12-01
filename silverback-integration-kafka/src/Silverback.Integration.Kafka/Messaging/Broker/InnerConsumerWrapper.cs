using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    internal delegate void MessageReceivedHandler(Confluent.Kafka.Message<byte[], byte[]> message, int retryCount);

    internal class InnerConsumerWrapper : IDisposable
    {
        private readonly List<KafkaConsumerEndpoint> _endpoints = new List<KafkaConsumerEndpoint>();
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private readonly Dictionary<Confluent.Kafka.TopicPartition, int> _retryCountDictionary = new Dictionary<Confluent.Kafka.TopicPartition, int>();
        private readonly Dictionary<Confluent.Kafka.TopicPartition, long> _lastOffsetDictionary = new Dictionary<Confluent.Kafka.TopicPartition, long>();
        
        private Confluent.Kafka.Consumer<byte[], byte[]> _innerConsumer;

        private bool _started;
        private bool _disposing;

        public InnerConsumerWrapper(Confluent.Kafka.Consumer<byte[], byte[]> innerConsumer, CancellationToken cancellationToken, ILogger logger)
        {
            _innerConsumer = innerConsumer;
            _cancellationToken = cancellationToken;
            _logger = logger;
        }

        public event MessageReceivedHandler Received;

        public void Subscribe(KafkaConsumerEndpoint endpoint)
        {
            _endpoints.Add(endpoint);

            if (!_innerConsumer.Subscription.Contains(endpoint.Name))
                _innerConsumer.Subscribe(endpoint.Name);
        }

        public async Task Commit(Confluent.Kafka.Message<byte[], byte[]> message)
        {
            var committedOffsets = await _innerConsumer.CommitAsync(message);
            _logger.LogTrace("Committed offset: {offset}", committedOffsets);
        }

        public void Seek(Confluent.Kafka.TopicPartitionOffset topicPartitionOffset) => _innerConsumer.Seek(topicPartitionOffset);

        public void StartConsuming()
        {
            if (_started)
                return;

            _innerConsumer.OnPartitionsAssigned += (_, partitions) =>
            {
                _logger.LogTrace("Assigned partitions '{partitions}' to member with name '{name}' and id '{memberId}'",
                    string.Join(", ", partitions), _innerConsumer.Name, _innerConsumer.MemberId);
                _innerConsumer.Assign(partitions);
            };

            _innerConsumer.OnPartitionsRevoked += (_, partitions) =>
            {
                _logger.LogTrace("Revoked '{partitions}' from member with name '{name}' and id '{memberId}'",
                    string.Join(", ", partitions), _innerConsumer.Name, _innerConsumer.MemberId);

                if (!_disposing) _innerConsumer?.Unassign();
            };

            Task.Run(() => Consume(), _cancellationToken);

            _started = true;
        }

        private void Consume()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (!_innerConsumer.Consume(out var message, TimeSpan.FromMilliseconds(_endpoints.Min(e => e.PollTimeout))))
                    continue;

                _logger.LogTrace("Consuming message: {topic} [{partition}] @{offset}", message.Topic, message.Partition, message.Offset);

                Received?.Invoke(message, GetRetryCount(message.TopicPartitionOffset));
            }
        }

        private int GetRetryCount(Confluent.Kafka.TopicPartitionOffset tpo)
        {
            if (!IsRetry(tpo))
            {
                _lastOffsetDictionary[tpo.TopicPartition] = tpo.Offset;
                return 0;
            }

            int retryCount;
            if (_lastOffsetDictionary.ContainsKey(tpo.TopicPartition))
                retryCount = _retryCountDictionary[tpo.TopicPartition] + 1;
            else
                retryCount = 1;

            _retryCountDictionary[tpo.TopicPartition] = retryCount;

            return retryCount;
        }

        private bool IsRetry(Confluent.Kafka.TopicPartitionOffset tpo)
        {
            if (!_lastOffsetDictionary.ContainsKey(tpo.TopicPartition))
                return false;

            return tpo.Offset == _lastOffsetDictionary[tpo.TopicPartition];
        }

        public void Dispose()
        {
            _disposing = true;

            _innerConsumer?.Unassign();
            _innerConsumer?.Unsubscribe();
            _innerConsumer?.Dispose();

            _innerConsumer = null;
        }
    }
}
