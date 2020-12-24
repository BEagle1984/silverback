// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class ConsumeLoopHandler : IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly IConsumer<byte[]?, byte[]?> _confluenceConsumer;

        private readonly ISilverbackIntegrationLogger _logger;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private ConsumerChannelsManager? _channelsManager;

        private CancellationTokenSource _cancellationTokenSource = new();

        private TaskCompletionSource<bool> _consumeTaskCompletionSource = new();

        public ConsumeLoopHandler(
            KafkaConsumer consumer,
            IConsumer<byte[]?, byte[]?> confluenceConsumer,
            ConsumerChannelsManager? channelsManager,
            ISilverbackIntegrationLogger logger)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _confluenceConsumer = Check.NotNull(confluenceConsumer, nameof(confluenceConsumer));
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

        public void SetChannelsManager(ConsumerChannelsManager channelsManager) => _channelsManager = channelsManager;

        public void Dispose()
        {
            AsyncHelper.RunSynchronously(StopAsync);
            _cancellationTokenSource.Dispose();
        }

        private async Task ConsumeAsync(
            TaskCompletionSource<bool> taskCompletionSource,
            CancellationToken cancellationToken)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!ConsumeOnce(cancellationToken))
                    break;
            }

            taskCompletionSource.TrySetResult(true);

            // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
            // synchronously to stay within a single long-running thread with the Consume loop.
            if (!cancellationToken.IsCancellationRequested)
                await _consumer.DisconnectAsync().ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private bool ConsumeOnce(CancellationToken cancellationToken)
        {
            try
            {
                var consumeResult = _confluenceConsumer.Consume(cancellationToken);

                if (consumeResult == null)
                    return true;

                _logger.LogDebug(
                    KafkaEventIds.ConsumingMessage,
                    "Consuming message: {topic}[{partition}]@{offset}. (consumerId: {consumerId})",
                    consumeResult.Topic,
                    consumeResult.Partition.Value,
                    consumeResult.Offset,
                    _consumer.Id);

                if (_channelsManager == null)
                {
                    _logger.LogTrace(
                        IntegrationEventIds.LowLevelTracing,
                        "Waiting for channels manager to be initialized... (consumerId: {consumerId})",
                        _consumer.Id);

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
                {
                    _logger.LogTrace(
                        KafkaEventIds.ConsumingCanceled,
                        "Consuming canceled. (consumerId: {consumerId})",
                        _consumer.Id);
                }
            }
            catch (KafkaException ex)
            {
                if (!_consumer.AutoRecoveryIfEnabled(ex, cancellationToken))
                    return false;
            }
            catch (Exception ex)
            {
                if (!(ex is ConsumerPipelineFatalException))
                {
                    _logger.LogCritical(
                        IntegrationEventIds.ConsumerFatalError,
                        ex,
                        "Fatal error occurred while consuming. The consumer will be stopped. (consumerId: {consumerId})",
                        _consumer.Id);
                }

                return false;
            }

            return true;
        }
    }
}
