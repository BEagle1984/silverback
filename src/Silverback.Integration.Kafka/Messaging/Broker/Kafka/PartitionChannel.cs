// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class PartitionChannel : IDisposable
    {
        private readonly KafkaConsumer _consumer;

        private readonly ISilverbackLogger _logger;

        private readonly object _readingLock = new();

        private Channel<ConsumeResult<byte[]?, byte[]?>> _channel;

        private TaskCompletionSource<bool> _readTaskCompletionSource = new();

        private CancellationTokenSource _readCancellationTokenSource = new();

        private bool _disposed;

        public PartitionChannel(KafkaConsumer consumer, TopicPartition topicPartition, ISilverbackLogger logger)
        {
            _consumer = consumer;
            TopicPartition = topicPartition;
            _logger = logger;

            _channel = CreateBoundedChannel();
        }

        public TopicPartition TopicPartition { get; }

        public ChannelReader<ConsumeResult<byte[]?, byte[]?>> Reader => _channel.Reader;

        public ChannelWriter<ConsumeResult<byte[]?, byte[]?>> Writer => _channel.Writer;

        public CancellationToken ReadCancellationToken => _readCancellationTokenSource.Token;

        public Task ReadTask => _readTaskCompletionSource.Task;

        public bool IsReading { get; private set; }

        public void Reset()
        {
            Writer.Complete();
            _channel = CreateBoundedChannel();
        }

        public bool StartReading()
        {
            lock (_readingLock)
            {
                if (IsReading)
                    return false;

                IsReading = true;
            }

            if (_readCancellationTokenSource.IsCancellationRequested)
            {
                _readCancellationTokenSource.Dispose();
                _readCancellationTokenSource = new CancellationTokenSource();
            }

            if (_readTaskCompletionSource.Task.IsCompleted)
                _readTaskCompletionSource = new TaskCompletionSource<bool>();

            return true;
        }

        public Task StopReadingAsync()
        {
            if (!_readCancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Stopping channel processing loop for partition {topic}[{partition}].",
                    () => new object[] { TopicPartition.Topic, TopicPartition.Partition });

                _readCancellationTokenSource.Cancel();
            }

            lock (_readingLock)
            {
                if (!IsReading)
                    _readTaskCompletionSource.TrySetResult(true);
            }

            return _readTaskCompletionSource.Task;
        }

        public void NotifyReadingStopped(bool hasThrown)
        {
            lock (_readingLock)
            {
                if (!IsReading)
                    return;

                IsReading = false;
                _readTaskCompletionSource.TrySetResult(!hasThrown);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _readCancellationTokenSource.Dispose();

            _disposed = true;
        }

        // TODO: Can test setting for backpressure limit?
        private Channel<ConsumeResult<byte[]?, byte[]?>> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumeResult<byte[]?, byte[]?>>(_consumer.Endpoint.BackpressureLimit);
    }
}
