// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class ConsumerChannelsManager : ConsumerChannelsManager<ConsumeResult<byte[]?, byte[]?>>
    {
        private readonly IList<TopicPartition> _partitions;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        private readonly IBrokerCallbacksInvoker _callbacksInvoker;

        private readonly ISilverbackLogger _logger;

        private readonly CancellationTokenSource[] _readCancellationTokenSource;

        private readonly TaskCompletionSource<bool>[] _readTaskCompletionSources;

        private readonly SemaphoreSlim? _messagesLimiterSemaphoreSlim;

        private readonly SemaphoreSlim _readingSemaphoreSlim = new(1, 1);

        public ConsumerChannelsManager(
            IReadOnlyList<TopicPartition> partitions,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            Func<ISequenceStore> sequenceStoreFactory,
            ISilverbackLogger logger)
            : base(
                GetChannelsCount(partitions, consumer),
                consumer.Endpoint.BackpressureLimit,
                sequenceStoreFactory)
        {
            // Copy the partitions array to avoid concurrency issues if a rebalance occurs while initializing
            _partitions = Check.NotNull(partitions, nameof(partitions)).ToList();
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
            _logger = Check.NotNull(logger, nameof(logger));

            if (consumer.Endpoint.MaxDegreeOfParallelism < Channels.Length)
            {
                _messagesLimiterSemaphoreSlim = new SemaphoreSlim(
                    consumer.Endpoint.MaxDegreeOfParallelism,
                    consumer.Endpoint.MaxDegreeOfParallelism);
            }

            _readCancellationTokenSource = Enumerable.Range(0, Channels.Length)
                .Select(_ => new CancellationTokenSource()).ToArray();
            _readTaskCompletionSources = Enumerable.Range(0, Channels.Length)
                .Select(_ => new TaskCompletionSource<bool>()).ToArray();
            IsReading = new bool[Channels.Length];
        }

        public Task Stopping =>
            Task.WhenAll(_readTaskCompletionSources.Select(taskCompletionSource => taskCompletionSource.Task));

        public bool[] IsReading { get; }

        public void StartReading()
        {
            if (_consumer.Endpoint.ProcessPartitionsIndependently)
                _partitions.ForEach(StartReading);
            else if (_partitions.Count > 0)
                StartReading(0);
        }

        public void StartReading(TopicPartition topicPartition) => StartReading(GetChannelIndex(topicPartition));

        public Task StopReadingAsync() => Task.WhenAll(_partitions.Select(StopReadingAsync));

        public async Task StopReadingAsync(TopicPartition topicPartition)
        {
            int channelIndex = GetChannelIndex(topicPartition);

            // Needed because aborting the sequence causes a rollback that calls this method
            // again
            if (!IsReading[channelIndex])
                return;

            await _readingSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!_readCancellationTokenSource[channelIndex].IsCancellationRequested)
                {
                    _logger.LogConsumerLowLevelTrace(
                        _consumer,
                        "Stopping channel {channelIndex} processing loop.",
                        () => new object[] { channelIndex });

                    _readCancellationTokenSource[channelIndex].Cancel();
                }

                if (!IsReading[channelIndex])
                    _readTaskCompletionSources[channelIndex].TrySetResult(true);

                await SequenceStores[channelIndex].AbortAllAsync(SequenceAbortReason.ConsumerAborted).ConfigureAwait(false);

                await _readTaskCompletionSources[channelIndex].Task.ConfigureAwait(false);
            }
            finally
            {
                _readingSemaphoreSlim.Release();
            }
        }

        public void ResetChannel(TopicPartition topicPartition) => ResetChannel(GetChannelIndex(topicPartition));

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        public void Write(ConsumeResult<byte[]?, byte[]?> consumeResult, CancellationToken cancellationToken)
        {
            int channelIndex = GetChannelIndex(consumeResult.TopicPartition);

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Writing message ({topic}[{partition}]@{offset}) to channel {channelIndex}.",
                () => new object[]
                {
                    consumeResult.Topic,
                    consumeResult.Partition.Value,
                    consumeResult.Offset,
                    channelIndex
                });

            AsyncHelper.RunValueTaskSynchronously(
                () =>
                    Channels[channelIndex].Writer.WriteAsync(consumeResult, cancellationToken));
        }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            AsyncHelper.RunSynchronously(StopReadingAsync);
            _readCancellationTokenSource.ForEach(cancellationTokenSource => cancellationTokenSource.Dispose());

            _logger.LogConsumerLowLevelTrace(_consumer, "All channels reader cancellation tokens disposed.");

            _messagesLimiterSemaphoreSlim?.Dispose();
            _readingSemaphoreSlim.Dispose();
        }

        private static int GetChannelsCount(IReadOnlyList<TopicPartition> partitions, KafkaConsumer consumer) =>
            consumer.Endpoint.ProcessPartitionsIndependently
                ? partitions.Count
                : Math.Min(partitions.Count, 1);

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        private void StartReading(int channelIndex)
        {
            _readingSemaphoreSlim.Wait();

            try
            {
                // Clear the current activity to ensure we don't propagate the previous traceId (e.g. when restarting because of a rollback)
                Activity.Current = null;

                if (_readCancellationTokenSource[channelIndex].IsCancellationRequested &&
                    !_readTaskCompletionSources[channelIndex].Task.IsCompleted)
                {
                    _logger.LogConsumerLowLevelTrace(
                        _consumer,
                        "Deferring channel {channelIndex} processing loop startup...",
                        () => new object[] { channelIndex });

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

                Task.Run(() => ReadChannelAsync(channelIndex, _readCancellationTokenSource[channelIndex].Token));
            }
            finally
            {
                _readingSemaphoreSlim.Release();
            }
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ReadChannelAsync(int channelIndex, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Starting channel {channelIndex} processing loop...",
                    () => new object[] { channelIndex });

                while (!cancellationToken.IsCancellationRequested)
                {
                    await ReadChannelOnceAsync(channelIndex, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
                _logger.LogConsumerLowLevelTrace(
                    _consumer,
                    "Exiting channel {channelIndex} processing loop (operation canceled).",
                    () => new object[] { channelIndex });
            }
            catch (Exception ex)
            {
                if (ex is not ConsumerPipelineFatalException)
                    _logger.LogConsumerFatalError(_consumer, ex);

                IsReading[channelIndex] = false;
                _readTaskCompletionSources[channelIndex].TrySetResult(false);

                await _consumer.DisconnectAsync().ConfigureAwait(false);
            }

            IsReading[channelIndex] = false;
            _readTaskCompletionSources[channelIndex].TrySetResult(true);

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Exited channel {channelIndex} processing loop.",
                () => new object[] { channelIndex });
        }

        private async Task ReadChannelOnceAsync(int channelIndex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Reading channel {channelIndex}...",
                () => new object[] { channelIndex });

            var consumeResult = await Channels[channelIndex].Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (consumeResult.IsPartitionEOF)
            {
                _logger.LogEndOfPartition(consumeResult, _consumer);
                _callbacksInvoker.Invoke<IKafkaPartitionEofCallback>(
                    handler => handler.OnEndOfTopicPartitionReached(
                        consumeResult.TopicPartition,
                        _consumer));
                return;
            }

            if (_messagesLimiterSemaphoreSlim != null)
                await _messagesLimiterSemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _consumer.HandleMessageAsync(
                        consumeResult.Message,
                        consumeResult.TopicPartitionOffset,
                        SequenceStores[channelIndex])
                    .ConfigureAwait(false);
            }
            finally
            {
                _messagesLimiterSemaphoreSlim?.Release();
            }
        }

        private int GetChannelIndex(TopicPartition topicPartition) =>
            _consumer.Endpoint.ProcessPartitionsIndependently
                ? GetPartitionAssignmentIndex(topicPartition)
                : 0;

        private int GetPartitionAssignmentIndex(TopicPartition topicPartition) =>
            _partitions.IndexOf(topicPartition);
    }
}
