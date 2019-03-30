// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal class InnerConsumerWrapper : IDisposable
    {
        private readonly List<KafkaConsumerEndpoint> _endpoints = new List<KafkaConsumerEndpoint>();
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private Confluent.Kafka.IConsumer<byte[], byte[]> _innerConsumer;

        private bool _consumed;
        private bool _started;

        public InnerConsumerWrapper(Confluent.Kafka.ConsumerConfig config, CancellationToken cancellationToken, ILogger logger)
        {
            _innerConsumer = BuildConfluentConsumer(config);
            _cancellationToken = cancellationToken;
            _logger = logger;
        }

        public event MessageReceivedHandler Received;

        public void Subscribe(KafkaConsumerEndpoint endpoint)
        {
            _endpoints.Add(endpoint);

            _innerConsumer.Subscribe(_endpoints.Select(e => e.Name));
        }

        public void Commit(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets)
        {
            _innerConsumer.Commit(offsets);
        }

        public void StoreOffset(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets) =>
            offsets.ForEach(_innerConsumer.StoreOffset);
        
        public void CommitAll(CancellationToken cancellationToken = default)
        {
            if (!_consumed) return;
            
            _innerConsumer.Commit();
        }

        public void Seek(Confluent.Kafka.TopicPartitionOffset tpo) => _innerConsumer.Seek(tpo);

        public void StartConsuming()
        {
            if (_started)
                return;

            Task.Run(Consume, _cancellationToken);

            _started = true;
        }

        private Confluent.Kafka.IConsumer<byte[], byte[]> BuildConfluentConsumer(Confluent.Kafka.ConsumerConfig config)
        {
            return new Confluent.Kafka.ConsumerBuilder<byte[], byte[]>(config)
                .SetPartitionsAssignedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                        _logger.LogTrace("Assigned topic {topic} partition {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, _innerConsumer.MemberId));
                })
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                        _logger.LogTrace("Revoked topic {topic} partition {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, _innerConsumer.MemberId));
                })
                .SetOffsetsCommittedHandler((_, offsets) =>
                {
                    foreach (var offset in offsets.Offsets)
                    {
                        if (offset.Offset == Confluent.Kafka.Offset.Unset)
                            continue;

                        if (offset.Error != null && offset.Error.Code != Confluent.Kafka.ErrorCode.NoError)
                        {
                            _logger.LogError(
                                "Error occurred committing the offset {topic} {partition} @{offset}: {errorCode} - {errorReason} ",
                                offset.Topic, offset.Partition, offset.Offset, offset.Error.Code, offset.Error.Reason);
                        }
                        else
                        {
                            _logger.LogTrace("Successfully committed offset {topic} {partition} @{offset}.",
                                offset.Topic, offset.Partition, offset.Offset);
                        }
                    }
                })
                .SetErrorHandler((_, e) =>
                {
                    _logger.Log(e.IsFatal ? LogLevel.Critical : LogLevel.Warning,
                            "Error in Kafka consumer: {reason}.", e.Reason);
                })
                .SetStatisticsHandler((_, json) =>
                {
                    _logger.LogInformation($"Statistics: {json}");
                })
                .Build();
        }

        private void Consume()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _innerConsumer.Consume(_cancellationToken);
                    _consumed = true;

                    _logger.LogTrace("Consuming message: {topic} {partition} @{offset}", result.Topic, result.Partition, result.Offset);

                    Received?.Invoke(result.Message, result.TopicPartitionOffset);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Consuming cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex,
                        "Fatal error occurred consuming a message." +
                        "The consumer will be stopped. See inner exception for details.");
                    break;
                }
            }
        }
        
        public void Dispose()
        {
            _innerConsumer?.Close();
            _innerConsumer?.Dispose();

            _innerConsumer = null;
        }
    }
}
