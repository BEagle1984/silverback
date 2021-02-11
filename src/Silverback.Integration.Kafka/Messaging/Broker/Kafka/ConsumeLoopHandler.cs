// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class ConsumeLoopHandler : IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        private readonly ISilverbackLogger _logger;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private ConsumerChannelsManager? _channelsManager;

        private CancellationTokenSource _cancellationTokenSource = new();

        private TaskCompletionSource<bool> _consumeTaskCompletionSource = new();

        public ConsumeLoopHandler(
            KafkaConsumer consumer,
            ConsumerChannelsManager? channelsManager,
            ISilverbackLogger logger)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _channelsManager = channelsManager;
            _logger = Check.NotNull(logger, nameof(logger));
        }

        public Task Stopping => _consumeTaskCompletionSource.Task;

        public bool IsConsuming { get; private set; }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public void Start()
        {
            if (IsConsuming)
                return;

            IsConsuming = true;

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }

            if (_consumeTaskCompletionSource.Task.IsCompleted)
                _consumeTaskCompletionSource = new TaskCompletionSource<bool>();

            var taskCompletionSource = _consumeTaskCompletionSource;
            var cancellationToken = _cancellationTokenSource.Token;

            Task.Factory.StartNew(
                () => ConsumeAsync(taskCompletionSource, cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

            if (!IsConsuming)
                _consumeTaskCompletionSource.TrySetResult(true);

            IsConsuming = false;

            return Stopping;
        }

        public void SetChannelsManager(ConsumerChannelsManager channelsManager) =>
            _channelsManager = channelsManager;

        public void Dispose()
        {
            AsyncHelper.RunSynchronously(StopAsync);
            _cancellationTokenSource.Dispose();
        }

        private async Task ConsumeAsync(
            TaskCompletionSource<bool> taskCompletionSource,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!ConsumeOnce(cancellationToken))
                    break;
            }

            _logger.LogConsumerLowLevelTrace(_consumer, "Consume loop stopped.");

            taskCompletionSource.TrySetResult(true);

            // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
            // synchronously to stay within a single long-running thread with the Consume loop.
            if (!cancellationToken.IsCancellationRequested)
                await _consumer.DisconnectAsync().ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        [SuppressMessage("", "CA1508", Justification = "_channelsManager is set on partitions assignment")]
        private bool ConsumeOnce(CancellationToken cancellationToken)
        {
            try
            {
                if (_consumer.ConfluentConsumer == null)
                {
                    _logger.LogConsumerLowLevelTrace(_consumer, "KafkaConsumer.ConfluentConsumer is null.");
                    return false;
                }

                _logger.LogConsumerLowLevelTrace(_consumer, "Consume next message.");

                var consumeResult = _consumer.ConfluentConsumer.Consume(cancellationToken);

                if (consumeResult == null)
                    return true;

                _logger.LogConsuming(consumeResult, _consumer);

                if (_channelsManager == null)
                {
                    _logger.LogConsumerLowLevelTrace(
                        _consumer,
                        "Waiting for channels manager to be initialized...");

                    // Wait until the ChannelsManager is set (after the partitions have been assigned)
                    while (_channelsManager == null)
                    {
                        Task.Delay(50, cancellationToken).Wait(cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                _channelsManager.Write(consumeResult, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    _logger.LogConsumingCanceled(_consumer);
            }
            catch (Exception ex)
            {
                if (!_consumer.AutoRecoveryIfEnabled(ex, cancellationToken))
                    return false;
            }

            return true;
        }
    }
}
