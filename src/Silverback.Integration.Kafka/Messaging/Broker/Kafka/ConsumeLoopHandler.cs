// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class ConsumeLoopHandler : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly KafkaConsumer _consumer;

    private readonly ISilverbackLogger _logger;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly ConsumerChannelsManager _channelsManager;

    private readonly OffsetsTracker? _offsetsTracker;

    private CancellationTokenSource _cancellationTokenSource = new();

    private TaskCompletionSource<bool>? _consumeTaskCompletionSource;

    private bool _isDisposed;

    public ConsumeLoopHandler(
        KafkaConsumer consumer,
        ConsumerChannelsManager channelsManager,
        OffsetsTracker? offsetsTracker,
        ISilverbackLogger logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _channelsManager = Check.NotNull(channelsManager, nameof(channelsManager));
        _offsetsTracker = offsetsTracker;
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task Stopping => _consumeTaskCompletionSource?.Task ?? Task.CompletedTask;

    public bool IsConsuming { get; private set; }

    public void Start()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

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

        TaskCompletionSource<bool>? taskCompletionSource = _consumeTaskCompletionSource;
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        Task.Factory.StartNew(
                () => ConsumeAsync(taskCompletionSource, cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .FireAndForget();
    }

    public async Task StopAsync()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        if (!IsConsuming)
        {
            await Stopping.ConfigureAwait(false);
            return;
        }

        _logger.LogConsumerTrace(_consumer, "Stopping ConsumeLoopHandler... | instanceId: {instanceId}", () => [Id]);

        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);

        IsConsuming = false;

        await Stopping.ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogConsumerTrace(_consumer, "Disposing ConsumeLoopHandler... | instanceId: {instanceId}", () => [Id]);

        StopAsync().SafeWait();
        _cancellationTokenSource.Dispose();

        _logger.LogConsumerTrace(_consumer, "ConsumeLoopHandler disposed. | instanceId: {instanceId}", () => [Id]);

        _isDisposed = true;
    }

    private static ConsumeResult<byte[]?, byte[]?> Consume(IConfluentConsumerWrapper client, CancellationToken cancellationToken)
    {
        try
        {
            return client.Consume(cancellationToken);
        }
        catch (Exception)
        {
            // Ignore exceptions if the client is being disconnected
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    private async Task ConsumeAsync(TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken)
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;
        _logger.LogConsumerTrace(
            _consumer,
            "Starting consume loop... | instanceId: {instanceId}, taskId: {taskId}",
            () => [Id, taskCompletionSource.Task.Id]);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!ConsumeOnce(cancellationToken))
                break;
        }

        _logger.LogConsumerTrace(
            _consumer,
            "Consume loop stopped. | instanceId: {instanceId}, taskId: {taskId}",
            () => [Id, taskCompletionSource.Task.Id]);

        taskCompletionSource.TrySetResult(true);

        // There's unfortunately no async version of Confluent.Kafka.IConsumer.Consume() so we need to run
        // synchronously to stay within a single long-running thread with the Consume loop.
        // The call to DisconnectAsync is the only exception since we are exiting anyway and Consume will
        // not be called anymore.
        if (!cancellationToken.IsCancellationRequested)
            await _consumer.Client.DisconnectAsync().ConfigureAwait(false);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private bool ConsumeOnce(CancellationToken cancellationToken)
    {
        try
        {
            ConsumeResult<byte[]?, byte[]?> consumeResult = Consume(_consumer.Client, cancellationToken);

            _logger.LogConsuming(consumeResult, _consumer);

            _offsetsTracker?.TrackOffset(consumeResult.TopicPartitionOffset);
            _channelsManager.Write(consumeResult, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (ChannelClosedException ex)
        {
            // Ignore the ChannelClosedException as it might be thrown in case of retry
            // (see ConsumerChannelsManager.Reset method)
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (Exception ex)
        {
            AutoRecoveryIfEnabled(ex);
            return false;
        }

        return true;
    }

    private void AutoRecoveryIfEnabled(Exception ex)
    {
        if (!_consumer.Configuration.EnableAutoRecovery)
        {
            _logger.LogKafkaExceptionNoAutoRecovery(_consumer, ex);
            return;
        }

        _logger.LogKafkaExceptionAutoRecovery(_consumer, ex);

        _consumer.TriggerReconnectAsync().FireAndForget();
    }
}
