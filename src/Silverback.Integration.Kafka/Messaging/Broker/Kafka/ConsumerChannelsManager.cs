// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class ConsumerChannelsManager : IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        private readonly IBrokerCallbacksInvoker _callbacksInvoker;

        private readonly ISilverbackLogger _logger;

        private readonly ConcurrentDictionary<TopicPartition, PartitionChannel> _partitionChannels = new();

        private readonly SemaphoreSlim _messagesLimiterSemaphoreSlim;

        private bool _disposed;

        public ConsumerChannelsManager(
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
            _logger = Check.NotNull(logger, nameof(logger));

            _messagesLimiterSemaphoreSlim = new SemaphoreSlim(
                consumer.Configuration.MaxDegreeOfParallelism,
                consumer.Configuration.MaxDegreeOfParallelism);
        }

        public Task Stopping =>
            Task.WhenAll(_partitionChannels.Values.Select(channel => channel.ReadTask));

        public void StartReading(IEnumerable<TopicPartition> topicPartitions) =>
            topicPartitions.ForEach(StartReading);

        public void StartReading(TopicPartition topicPartition)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            var partitionChannel = GetPartitionChannel(topicPartition);

            // Clear the current activity to ensure we don't propagate the previous traceId (e.g. when restarting because of a rollback)
            Activity.Current = null;

            if (partitionChannel.ReadCancellationToken.IsCancellationRequested &&
                !partitionChannel.ReadTask.IsCompleted)
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Deferring channel processing loop startup for partition {topic}[{partition}]...",
                    () => new object[] { topicPartition.Topic, topicPartition.Partition });

                // If the cancellation is still pending await it and restart after successful stop
                Task.Run(
                        async () =>
                        {
                            await partitionChannel.ReadTask.ConfigureAwait(false);
                            StartReading(topicPartition);
                        })
                    .FireAndForget();

                return;
            }

            if (!partitionChannel.StartReading())
                return;

            Task.Run(() => ReadChannelAsync(partitionChannel)).FireAndForget();
        }

        public Task StopReadingAsync() =>
            Task.WhenAll(
                _partitionChannels.Values.Select(
                    partitionChannel => StopReadingAsync(partitionChannel.TopicPartition)));

        public Task StopReadingAsync(IEnumerable<TopicPartition> topicPartitions) =>
            Task.WhenAll(topicPartitions.Select(StopReadingAsync));

        public async Task StopReadingAsync(TopicPartition topicPartition)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            var partitionChannel = GetPartitionChannel(topicPartition, false);

            if (partitionChannel == null)
                return;

            await partitionChannel.StopReadingAsync().ConfigureAwait(false);
            _partitionChannels.TryRemove(topicPartition, out _);
            partitionChannel.Writer.TryComplete();
        }

        public void Reset(TopicPartition topicPartition)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_partitionChannels.TryGetValue(topicPartition, out var partitionChannel))
            {
                partitionChannel.Reset();
            }
        }

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        public void Write(ConsumeResult<byte[]?, byte[]?> consumeResult, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            var partitionChannel = GetPartitionChannel(consumeResult.TopicPartition);

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Writing message ({topic}[{partition}]@{offset}) to channel {topic}[{channelPartition}].",
                () => new object[]
                {
                    consumeResult.Topic,
                    consumeResult.Partition.Value,
                    consumeResult.Offset.Value,
                    partitionChannel.TopicPartition.Topic,
                    partitionChannel.TopicPartition.Partition.Value
                });

            AsyncHelper.RunSynchronously(
                () => partitionChannel.Writer.WriteAsync(consumeResult, cancellationToken).AsTask());
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            AsyncHelper.RunSynchronously(StopReadingAsync);
            _partitionChannels.Values.ForEach(partitionChannel => partitionChannel.Dispose());

            _logger.LogConsumerLowLevelTrace(_consumer, "All channels reader cancellation tokens disposed.");

            _messagesLimiterSemaphoreSlim.Dispose();

            _disposed = true;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync(PartitionChannel partitionChannel)
        {
            try
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Starting channel processing loop for partition {topic}[{partition}]...",
                    () => new object[]
                    {
                        partitionChannel.TopicPartition.Topic,
                        partitionChannel.TopicPartition.Partition.Value
                    });

                while (!partitionChannel.ReadCancellationToken.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync(partitionChannel).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Exiting channel processing loop for partition {topic}[{partition}] (operation canceled).",
                    () => new object[]
                    {
                        partitionChannel.TopicPartition.Topic,
                        partitionChannel.TopicPartition.Partition.Value
                    });
            }
            catch (Exception ex)
            {
                if (ex is not ConsumerPipelineFatalException)
                    _logger.LogConsumerFatalError(_consumer, ex);

                partitionChannel.NotifyReadingStopped(true);

                await _consumer.DisconnectAsync().ConfigureAwait(false);
            }

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Exited channel processing loop for partition {topic}[{partition}].",
                () => new object[]
                {
                    partitionChannel.TopicPartition.Topic,
                    partitionChannel.TopicPartition.Partition.Value
                });

            partitionChannel.NotifyReadingStopped(false);
        }

        private async Task ReadChannelOnceAsync(PartitionChannel partitionChannel)
        {
            partitionChannel.ReadCancellationToken.ThrowIfCancellationRequested();

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Reading channel for partition {topic}[{partition}]...",
                () => new object[]
                {
                    partitionChannel.TopicPartition.Topic,
                    partitionChannel.TopicPartition.Partition.Value
                });

            var consumeResult =
                await partitionChannel.Reader.ReadAsync(partitionChannel.ReadCancellationToken).ConfigureAwait(false);

            partitionChannel.ReadCancellationToken.ThrowIfCancellationRequested();

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogEndOfPartition(consumeResult, _consumer);
                _callbacksInvoker.Invoke<IKafkaPartitionEofCallback>(
                    handler => handler.OnEndOfTopicPartitionReached(
                        consumeResult.TopicPartition,
                        _consumer));
                return;
            }

            if (_partitionChannels.Count > _consumer.Configuration.MaxDegreeOfParallelism)
            {
                await _messagesLimiterSemaphoreSlim.WaitAsync(partitionChannel.ReadCancellationToken)
                    .ConfigureAwait(false);
            }

            try
            {
                await _consumer.HandleMessageAsync(consumeResult.Message, consumeResult.TopicPartitionOffset)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (_messagesLimiterSemaphoreSlim.CurrentCount < _consumer.Configuration.MaxDegreeOfParallelism)
                    _messagesLimiterSemaphoreSlim.Release();
            }
        }

        private PartitionChannel GetPartitionChannel(TopicPartition topicPartition) =>
            GetPartitionChannel(topicPartition, true)!;

        private PartitionChannel? GetPartitionChannel(TopicPartition topicPartition, bool create)
        {
            if (_partitionChannels.TryGetValue(topicPartition, out var partitionChannel))
                return partitionChannel;

            if (!create)
                return null;

            return _partitionChannels.GetOrAdd(
                topicPartition,
                new PartitionChannel(_consumer, topicPartition, _logger));
        }
    }
}
