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
        private readonly Confluent.Kafka.ConsumerConfig _config;
        private readonly bool _enableAutoRecovery;
        private readonly ILogger _logger;
        private readonly CancellationToken _cancellationToken;

        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(5);

        private Confluent.Kafka.IConsumer<byte[], byte[]> _innerConsumer;

        private bool _consumed;
        private bool _started;

        public InnerConsumerWrapper(Confluent.Kafka.ConsumerConfig config, bool enableAutoRecovery, CancellationToken cancellationToken, ILogger logger)
        {
            _config = config;
            _enableAutoRecovery = enableAutoRecovery;
            _cancellationToken = cancellationToken;
            _logger = logger;

            _innerConsumer = BuildConfluentConsumer(config);
        }

        public event KafkaMessageReceivedHandler Received;

        public void Subscribe(KafkaConsumerEndpoint endpoint)
        {
            _endpoints.Add(endpoint);

            Subscribe();
        }

        public void Commit(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets) => 
            _innerConsumer.Commit(offsets);

        public void StoreOffset(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets) =>
            offsets.ForEach(_innerConsumer.StoreOffset);
        
        public void CommitAll()
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

        private void Subscribe() => _innerConsumer.Subscribe(_endpoints.Select(e => e.Name));

        private Confluent.Kafka.IConsumer<byte[], byte[]> BuildConfluentConsumer(Confluent.Kafka.ConsumerConfig config)
        {
            return new Confluent.Kafka.ConsumerBuilder<byte[], byte[]>(config)
                .SetPartitionsAssignedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                        _logger.LogInformation("Assigned topic {topic} partition {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, _innerConsumer.MemberId));
                })
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                        _logger.LogInformation("Revoked topic {topic} partition {partition}, member id: {memberId}",
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
                    _logger.Log(e.IsFatal ? LogLevel.Critical : LogLevel.Error,
                            "Error in Kafka consumer: {reason}.", e.Reason);
                })
                .SetStatisticsHandler((_, json) =>
                {
                    _logger.LogInformation($"Statistics: {json}");
                })
                .Build();
        }

        private async Task Consume()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _innerConsumer.Consume(_cancellationToken);
                    _consumed = true;

                    if (result.IsPartitionEOF)
                    {
                        _logger.LogInformation("Partition EOF reached: {topic} {partition} @{offset}", result.Topic, result.Partition, result.Offset);
                        continue;
                    }

                    _logger.LogTrace("Consuming message: {topic} {partition} @{offset}", result.Topic, result.Partition, result.Offset);

                    if (Received != null)
                        await Received.Invoke(result.Message, result.TopicPartitionOffset);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Consuming canceled.");
                }
                catch (Confluent.Kafka.KafkaException ex)
                {
                    if (_enableAutoRecovery)
                    {
                        _logger.LogWarning(ex, "KafkaException occurred. The consumer will try to recover.");
                        ResetConsumer();
                    }
                    else
                    {
                        _logger.LogCritical(ex, "Fatal error occurred consuming a message. The consumer will be stopped. " +
                                                "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                                "(EnableAutoRecovery=true in the endpoint configuration).");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Fatal error occurred consuming a message. The consumer will be stopped.");
                    break;
                }
            }
        }

        private void ResetConsumer()
        {
            try
            {
                DisposeInnerConsumer();
                _innerConsumer = BuildConfluentConsumer(_config);
                Subscribe();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Failed to recover from consumer exception. Will retry in {_recoveryDelay.TotalSeconds} seconds.");
                Thread.Sleep(_recoveryDelay);
                ResetConsumer();
            }
        }

        private void DisposeInnerConsumer()
        {
            _innerConsumer?.Close();
            _innerConsumer?.Dispose();

            _innerConsumer = null;
        }

        public void Dispose()
        {
            DisposeInnerConsumer();
        }
    }
}
