// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
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

        private TaskCompletionSource<bool>? _consumeTaskCompletionSource;

        public ConsumeLoopHandler(
            KafkaConsumer consumer,
            ConsumerChannelsManager? channelsManager,
            ISilverbackLogger logger)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _channelsManager = channelsManager;
            _logger = Check.NotNull(logger, nameof(logger));
        }

        public InstanceIdentifier Id { get; } = new();

        public Task Stopping => _consumeTaskCompletionSource?.Task ?? Task.CompletedTask;

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

            if (_consumeTaskCompletionSource == null || _consumeTaskCompletionSource.Task.IsCompleted)
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
            if (!IsConsuming)
                return Stopping;

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Stopping ConsumeLoopHandler... | instanceId: {instanceId}",
                () => new object[] { Id });

            _cancellationTokenSource.Cancel();

            IsConsuming = false;

            return Stopping;
        }

        public void SetChannelsManager(ConsumerChannelsManager channelsManager) =>
            _channelsManager = channelsManager;

        public void Dispose()
        {
            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Disposing ConsumeLoopHandler... | instanceId: {instanceId}",
                () => new object[] { Id });

            AsyncHelper.RunSynchronously(StopAsync);
            _cancellationTokenSource.Dispose();

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "ConsumeLoopHandler disposed. | instanceId: {instanceId}",
                () => new object[] { Id });
        }

        private async Task ConsumeAsync(
            TaskCompletionSource<bool> taskCompletionSource,
            CancellationToken cancellationToken)
        {
            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Starting consume loop... | instanceId: {instanceId}, taskId: {taskId}",
                () => new object[]
                {
                    Id,
                    taskCompletionSource.Task.Id
                });

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!ConsumeOnce(cancellationToken))
                    break;
            }

            _logger.LogConsumerLowLevelTrace(
                _consumer,
                "Consume loop stopped. | instanceId: {instanceId}, taskId: {taskId}",
                () => new object[]
                {
                    Id,
                    taskCompletionSource.Task.Id
                });

            taskCompletionSource.TrySetResult(true);

            // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
            // synchronously to stay within a single long-running thread with the Consume loop.
            // The call to DisconnectAsync is the only exception since we are exiting anyway and Consume will
            // not be called anymore.
            if (!cancellationToken.IsCancellationRequested)
                await _consumer.DisconnectAsync().ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        [SuppressMessage("", "CA1508", Justification = "_channelsManager is set on partitions assignment")]
        private bool ConsumeOnce(CancellationToken cancellationToken)
        {
            try
            {
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
            catch (ChannelClosedException)
            {
                // Ignore the ChannelClosedException as it might be thrown in case of retry
                // (see ConsumerChannelsManager.Reset method)
            }
            catch (Exception ex) when (ex is not ChannelClosedException)
            {
                AutoRecoveryIfEnabled(ex, cancellationToken);
                return false;
            }

            return true;
        }

        private void AutoRecoveryIfEnabled(Exception ex, CancellationToken cancellationToken)
        {
            if (!_consumer.Endpoint.Configuration.EnableAutoRecovery)
            {
                _logger.LogKafkaExceptionNoAutoRecovery(_consumer, ex);
                return;
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            _logger.LogKafkaExceptionAutoRecovery(_consumer, ex);

            _consumer.TriggerReconnectAsync().FireAndForget();
        }
    }
}
