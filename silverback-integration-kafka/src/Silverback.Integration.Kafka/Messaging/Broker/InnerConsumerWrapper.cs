// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    internal class InnerConsumerWrapper : IDisposable
    {
        private readonly List<KafkaConsumerEndpoint> _endpoints = new List<KafkaConsumerEndpoint>();
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private readonly Dictionary<Confluent.Kafka.TopicPartition, int> _retryCountDictionary = new Dictionary<Confluent.Kafka.TopicPartition, int>();
        private readonly Dictionary<Confluent.Kafka.TopicPartition, long> _lastOffsetDictionary = new Dictionary<Confluent.Kafka.TopicPartition, long>();
        
        private Confluent.Kafka.Consumer<byte[], byte[]> _innerConsumer;

        private bool _started;

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

            _innerConsumer.Subscribe(_endpoints.Select(e => e.Name));
        }

        public void Commit(Confluent.Kafka.TopicPartitionOffset tpo)
        {
            _innerConsumer.Commit(new []{ tpo }, _cancellationToken);
            _logger.LogTrace("Committed offset: {offset}", tpo);
        }

        public void Seek(Confluent.Kafka.TopicPartitionOffset tpo) => _innerConsumer.Seek(tpo);

        public void StartConsuming()
        {
            if (_started)
                return;

            AttachEventHandlers();

            Task.Run(() => Consume(), _cancellationToken);

            _started = true;
        }

        private void AttachEventHandlers()
        {
            _innerConsumer.OnPartitionsAssigned += (_, partitions) =>
                _logger.LogTrace("Assigned partitions: [{partitions}], member id: {memberId}",
                    string.Join(", ", partitions), _innerConsumer.MemberId);

            _innerConsumer.OnPartitionsRevoked += (_, partitions) =>
                _logger.LogTrace("Revoked partitions: [{partitions}]", string.Join(", ", partitions));

            _innerConsumer.OnPartitionEOF += (_, tpo) =>
                _logger.LogTrace(
                    "Reached end of topic {topic} partition {partition}, next message will be at offset {offset}.",
                    tpo.Topic, tpo.Partition, tpo.Offset);

            _innerConsumer.OnError += (_, e) =>
                _logger.Log(e.IsFatal ? LogLevel.Critical : LogLevel.Warning, "Error in Kafka consumer: {reason}.",
                    e.Reason);

            _innerConsumer.OnStatistics += (_, json) => _logger.LogInformation($"Statistics: {json}");
        }

        private void Consume()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _innerConsumer.Consume(_cancellationToken);

                    _logger.LogTrace("Consuming message: {topic} [{partition}] @{offset}", result.Topic, result.Partition, result.Offset);

                    Received?.Invoke(result.Message, result.TopicPartitionOffset, GetRetryCount(result.TopicPartitionOffset));
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Consuming cancelled.");
                }
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
            _innerConsumer?.Close();
            _innerConsumer?.Dispose();

            _innerConsumer = null;
        }
    }
}
