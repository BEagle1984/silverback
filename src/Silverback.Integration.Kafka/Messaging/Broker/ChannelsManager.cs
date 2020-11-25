// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    internal sealed class ChannelsManager : IDisposable
    {
        private readonly IList<TopicPartition> _partitions;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        private readonly IList<ISequenceStore> _sequenceStores;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private readonly ISilverbackIntegrationLogger _logger;

        private readonly Channel<ConsumeResult<byte[]?, byte[]?>>[] _channels;

        private readonly CancellationTokenSource[] _readCancellationTokenSource;

        private readonly TaskCompletionSource<bool>[] _readTaskCompletionSources;

        private readonly SemaphoreSlim? _messagesLimiterSemaphoreSlim;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ChannelsManager(
            IList<TopicPartition> partitions,
            KafkaConsumer consumer,
            IList<ISequenceStore> sequenceStores,
            ISilverbackIntegrationLogger logger)
        {
            // Copy the partitions array to avoid concurrency issues if a rebalance occurs while initializing
            _partitions = Check.NotNull(partitions, nameof(partitions)).ToList();
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _sequenceStores = Check.NotNull(sequenceStores, nameof(sequenceStores));
            _logger = Check.NotNull(logger, nameof(logger));

            _channels = consumer.Endpoint.ProcessPartitionsIndependently
                ? new Channel<ConsumeResult<byte[]?, byte[]?>>[partitions.Count]
                : new Channel<ConsumeResult<byte[]?, byte[]?>>[1];

            if (consumer.Endpoint.MaxDegreeOfParallelism < _channels.Length)
            {
                _messagesLimiterSemaphoreSlim = new SemaphoreSlim(
                    consumer.Endpoint.MaxDegreeOfParallelism,
                    consumer.Endpoint.MaxDegreeOfParallelism);
            }

            _readCancellationTokenSource = new CancellationTokenSource[_channels.Length];
            _readTaskCompletionSources = new TaskCompletionSource<bool>[_channels.Length];
            IsReading = new bool[_channels.Length];

            consumer.CreateSequenceStores(_channels.Length);

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = CreateBoundedChannel();
                _readCancellationTokenSource[i] = new CancellationTokenSource();
                _readTaskCompletionSources[i] = new TaskCompletionSource<bool>();
            }
        }

        public Task Stopping =>
            Task.WhenAll(_readTaskCompletionSources.Select(taskCompletionSource => taskCompletionSource.Task));

        public bool[] IsReading { get; }

        public void StartReading()
        {
            if (_consumer.Endpoint.ProcessPartitionsIndependently)
                _partitions.ForEach(StartReading);
            else
                StartReading(0);
        }

        public void StartReading(TopicPartition topicPartition) =>
            StartReading(GetChannelIndex(topicPartition));

        public void StopReading() => _partitions.ForEach(StopReading);

        public void StopReading(TopicPartition topicPartition)
        {
            int channelIndex = GetChannelIndex(topicPartition);
            _readCancellationTokenSource[channelIndex].Cancel();

            if (!IsReading[channelIndex])
                _readTaskCompletionSources[channelIndex].TrySetResult(true);
        }

        public ISequenceStore GetSequenceStore(TopicPartition topicPartition) =>
            _sequenceStores[GetChannelIndex(topicPartition)];

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        public void Write(ConsumeResult<byte[]?, byte[]?> consumeResult, CancellationToken cancellationToken)
        {
            int channelIndex = GetChannelIndex(consumeResult.TopicPartition);

            _logger.LogDebug(
                KafkaEventIds.ConsumingMessage,
                "Writing message ({topic}[{partition}] @{offset}) to channel {channelIndex}.",
                consumeResult.Topic,
                consumeResult.Partition,
                consumeResult.Offset,
                channelIndex);

            AsyncHelper.RunSynchronously(
                () => _channels[channelIndex].Writer.WriteAsync(consumeResult, cancellationToken));
        }

        public void Dispose()
        {
            StopReading();
            _readCancellationTokenSource.ForEach(cancellationTokenSource => cancellationTokenSource.Dispose());
            _messagesLimiterSemaphoreSlim?.Dispose();
        }

        private void StartReading(int channelIndex)
        {
            if (_readCancellationTokenSource[channelIndex].IsCancellationRequested &&
                !_readTaskCompletionSources[channelIndex].Task.IsCompleted)
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Deferring channel {channelIndex} processing loop startup...",
                    channelIndex);

                // If the cancellation is still pending await it and restart after successful stop
                Task.Run(
                    async () =>
                    {
                        await _readTaskCompletionSources[channelIndex].Task.ConfigureAwait(false);
                        StartReading(channelIndex);
                    });

                return;
            }

            if (IsReading[channelIndex])
                return;

            IsReading[channelIndex] = true;

            if (_readCancellationTokenSource[channelIndex].IsCancellationRequested)
            {
                _readCancellationTokenSource[channelIndex].Dispose();
                _readCancellationTokenSource[channelIndex] = new CancellationTokenSource();
            }

            if (_readTaskCompletionSources[channelIndex].Task.IsCompleted)
                _readTaskCompletionSources[channelIndex] = new TaskCompletionSource<bool>();

            var channelReader = _channels[channelIndex].Reader;

            if (channelReader.Completion.IsCompleted)
                _channels[channelIndex] = CreateBoundedChannel();

            Task.Run(
                () => ReadChannelAsync(
                    channelIndex,
                    channelReader,
                    _readCancellationTokenSource[channelIndex].Token));
        }

        // TODO: Can test setting for backpressure limit?
        private Channel<ConsumeResult<byte[]?, byte[]?>> CreateBoundedChannel() =>
            Channel.CreateBounded<ConsumeResult<byte[]?, byte[]?>>(_consumer.Endpoint.BackpressureLimit);

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync(
            int index,
            ChannelReader<ConsumeResult<byte[]?, byte[]?>> channelReader,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Starting channel {channelIndex} processing loop... (consumerId: {consumerId})",
                    index,
                    _consumer.Id);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync(channelReader, index, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogTrace(
                    IntegrationEventIds.LowLevelTracing,
                    "Exiting channel {channelIndex} processing loop (operation canceled). (consumerId: {consumerId})",
                    index,
                    _consumer.Id);
            }
            catch (Exception ex)
            {
                if (!(ex is ConsumerPipelineFatalException))
                {
                    _logger.LogCritical(
                        IntegrationEventIds.ConsumerFatalError,
                        ex,
                        "Fatal error occurred processing the consumed message. The consumer will be stopped. (consumerId: {consumerId})",
                        _consumer.Id);
                }

                IsReading[index] = false;
                _readTaskCompletionSources[index].TrySetResult(false);

                await _consumer.DisconnectAsync().ConfigureAwait(false);
            }

            IsReading[index] = false;
            _readTaskCompletionSources[index].TrySetResult(true);

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Exited channel {channelIndex} processing loop. (consumerId: {consumerId})",
                index,
                _consumer.Id);
        }

        private async Task ReadChannelOnceAsync(
            ChannelReader<ConsumeResult<byte[]?, byte[]?>> channelReader,
            int channelIndex,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogTrace(
                IntegrationEventIds.LowLevelTracing,
                "Reading channel {channelIndex}... (consumerId: {consumerId})",
                channelIndex,
                _consumer.Id);

            var consumeResult = await channelReader.ReadAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogInformation(
                    KafkaEventIds.EndOfPartition,
                    "Partition EOF reached: {topic}[{partition}]@{offset}. (consumerId: {consumerId})",
                    consumeResult.Topic,
                    consumeResult.Partition,
                    consumeResult.Offset,
                    _consumer.Id);
                return;
            }

            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!_consumer.Endpoint.Names.Any(
                endpointName =>
                    consumeResult.Topic.Equals(endpointName, StringComparison.OrdinalIgnoreCase)))
                return;

            if (_messagesLimiterSemaphoreSlim != null)
                await _messagesLimiterSemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _consumer.HandleMessageAsync(consumeResult.Message, consumeResult.TopicPartitionOffset)
                    .ConfigureAwait(false);
            }
            finally
            {
                _messagesLimiterSemaphoreSlim?.Release();
            }
        }

        private int GetChannelIndex(TopicPartition topicPartition) =>
            _consumer.Endpoint.ProcessPartitionsIndependently ? GetPartitionAssignmentIndex(topicPartition) : 0;

        private int GetPartitionAssignmentIndex(TopicPartition topicPartition) => _partitions.IndexOf(topicPartition);
    }
}
