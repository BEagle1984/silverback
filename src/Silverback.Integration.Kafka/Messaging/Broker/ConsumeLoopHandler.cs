// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal sealed class ConsumeLoopHandler : IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly KafkaConsumer _consumer;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly IConsumer<byte[]?, byte[]?> _confluenceConsumer;

        private readonly ISilverbackIntegrationLogger _logger;

        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private ChannelsManager? _channelsManager;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private TaskCompletionSource<bool> _consumeTaskCompletionSource = new TaskCompletionSource<bool>();

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ConsumeLoopHandler(
            KafkaConsumer consumer,
            IConsumer<byte[]?, byte[]?> confluenceConsumer,
            ChannelsManager? channelsManager,
            ISilverbackIntegrationLogger logger)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _confluenceConsumer = Check.NotNull(confluenceConsumer, nameof(confluenceConsumer));
            _channelsManager = channelsManager;
            _logger = Check.NotNull(logger, nameof(logger));
        }

        public bool IsConsuming { get; private set; }

        public Task Stopping => _consumeTaskCompletionSource.Task;

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

        public Task Stop()
        {
            _cancellationTokenSource.Cancel();

            if (!IsConsuming)
                _consumeTaskCompletionSource.TrySetResult(true);

            IsConsuming = false;

            return Stopping;
        }

        public void SetChannelsManager(ChannelsManager channelsManager) => _channelsManager = channelsManager;

        public void Dispose()
        {
            Stop();
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
                    "Consuming message: {topic} {partition} @{offset}.",
                    consumeResult.Topic,
                    consumeResult.Partition,
                    consumeResult.Offset);

                // Wait until the ChannelsManager is set (after the partitions have been assigned)
                while (_channelsManager == null)
                {
                    Task.Delay(50, cancellationToken).Wait(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                _channelsManager.Write(consumeResult, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    _logger.LogTrace(KafkaEventIds.ConsumingCanceled, "Consuming canceled.");
            }
            catch (KafkaException ex)
            {
                if (!_consumer.AutoRecoveryIfEnabled(ex, cancellationToken))
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    IntegrationEventIds.ConsumerFatalError,
                    ex,
                    "Fatal error occurred while consuming. The consumer will be stopped.");

                return false;
            }

            return true;
        }
    }
}
