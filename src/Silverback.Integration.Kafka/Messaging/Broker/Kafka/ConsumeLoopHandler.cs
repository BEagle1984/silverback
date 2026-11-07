// Copyright (c) 2026 Sergio Aquilini
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

    private readonly OffsetsTracker _offsetsTracker;

    private readonly System.Threading.Lock _lock = new(); // serializes the loop start and stop

    private CancellationTokenSource _cancellationTokenSource = new();

    private TaskCompletionSource<bool>? _consumeTaskCompletionSource;

    private TaskCompletionSource<bool>? _pendingPollTaskCompletionSource;

    private DateTime _lastSuccessfulConsume = DateTime.UtcNow;

    private bool _isDisposed;

    public ConsumeLoopHandler(
        KafkaConsumer consumer,
        ConsumerChannelsManager channelsManager,
        OffsetsTracker offsetsTracker,
        ISilverbackLogger logger)
    {
        _consumer = Check.NotNull(consumer, nameof(consumer));
        _channelsManager = Check.NotNull(channelsManager, nameof(channelsManager));
        _offsetsTracker = Check.NotNull(offsetsTracker, nameof(offsetsTracker));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    public Task Stopping => _consumeTaskCompletionSource?.Task ?? Task.CompletedTask;

    public bool IsConsuming { get; private set; }

    public void Start()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        Task previousLoop;
        TaskCompletionSource<bool> taskCompletionSource = new();
        CancellationToken cancellationToken;

        lock (_lock)
        {
            if (IsConsuming)
                return;

            IsConsuming = true;

            // The canceled source is disposed by the stop that canceled it, since its callbacks may still be running
            if (_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource = new CancellationTokenSource();

            // A previous loop may still be inside Consume after a non-blocking stop, so the new loop must wait for it to exit
            previousLoop = Stopping;
            _consumeTaskCompletionSource = taskCompletionSource;
            cancellationToken = _cancellationTokenSource.Token;
        }

        Task.Factory.StartNew(
                () => ConsumeAsync(previousLoop, taskCompletionSource, cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .FireAndForget();
    }

    public async Task StopAsync()
    {
        Check.ThrowObjectDisposedIf(_isDisposed, this);

        CancellationTokenSource? canceledTokenSource = null;
        Task cancellation = Task.CompletedTask;
        Task stopping;

        lock (_lock)
        {
            stopping = Stopping;

            // Flag the loop as stopped as soon as it is canceled, without waiting for the cancellation callbacks (e.g. of a write
            // waiting for room in a full channel): a start following a non-blocking stop must create a new loop
            if (IsConsuming)
            {
                IsConsuming = false;
                canceledTokenSource = _cancellationTokenSource;
                cancellation = canceledTokenSource.CancelAsync();
            }
        }

        if (canceledTokenSource != null)
        {
            _logger.LogConsumeLoopHandlerStopping(_consumer, Id);

            await cancellation.ConfigureAwait(false);
            canceledTokenSource.Dispose();

            _logger.LogConsumeLoopHandlerAwaitingStoppingToken(_consumer, Id);
        }

        await stopping.ConfigureAwait(false);
    }

    public Task WaitUntilCurrentPollCompletesAsync()
    {
        Task stopping = Stopping;

        if (stopping.IsCompleted)
            return Task.CompletedTask;

        TaskCompletionSource<bool>? completion = _pendingPollTaskCompletionSource;

        if (completion == null)
        {
            TaskCompletionSource<bool> newCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = Interlocked.CompareExchange(ref _pendingPollTaskCompletionSource, newCompletion, null) ?? newCompletion;
        }

        // Stopping also completes the wait if the last poll finished before this request was registered
        return Task.WhenAny(completion.Task, stopping);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogConsumeLoopHandlerDisposing(_consumer, Id);

        StopAsync().SafeWait();
        _cancellationTokenSource.Dispose();

        _logger.LogConsumeLoopHandlerDisposed(_consumer, Id);

        _isDisposed = true;
    }

    private async Task ConsumeAsync(Task previousLoop, TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken)
    {
        // Wait synchronously to keep polling on this long-running thread; the Confluent consumer must not be polled concurrently
        if (!previousLoop.IsCompleted)
            previousLoop.SafeWait();

        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;
        _logger.LogConsumeLoopStarting(_consumer, Id, taskCompletionSource.Task.Id);

        // Reset the last successful consume time to avoid false stall detection
        _lastSuccessfulConsume = DateTime.UtcNow;

        // The records buffered when the consumer was stopped were discarded: seek back to consume them again before polling, then
        // resume the partitions paused by a rollback that the stop defeated
        bool continueConsuming = RestorePartitions(cancellationToken);

        while (continueConsuming && !cancellationToken.IsCancellationRequested)
        {
            continueConsuming = ConsumeOnce(cancellationToken);
            NotifyPollCompleted();
        }

        _logger.LogConsumeLoopStopped(_consumer, Id, taskCompletionSource.Task.Id);

        taskCompletionSource.TrySetResult(true);
        NotifyPollCompleted();

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
            ConsumeResult<byte[]?, byte[]?>? consumeResult = Consume(_consumer.Client, cancellationToken);

            if (consumeResult == null)
                return CheckConsumerStall();

            _lastSuccessfulConsume = DateTime.UtcNow;
            _logger.LogConsuming(consumeResult, _consumer);

            // The end of partition is a position, not a record to be consumed again after a restart
            if (!consumeResult.IsPartitionEOF)
                _offsetsTracker.TrackOffset(consumeResult.TopicPartitionOffset);

            _channelsManager.Write(consumeResult, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (ChannelClosedException ex)
        {
            // The channel can be closed while a concurrent rollback or rebalance retires it
            _logger.LogConsumingCanceled(_consumer, ex);
        }
        catch (Exception ex)
        {
            AutoRecoveryIfEnabled(ex);
            return false;
        }

        return true;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private bool RestorePartitions(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return true;

        try
        {
            _consumer.SeekToUncommittedOffsets();
            _consumer.ResumeRollbackPausedPartitions();
            return true;
        }
        catch (Exception ex)
        {
            // Handled like a failing poll
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogConsumingCanceled(_consumer, ex);
                return true;
            }

            AutoRecoveryIfEnabled(ex);
            return false;
        }
    }

    private void NotifyPollCompleted()
    {
        if (_pendingPollTaskCompletionSource != null)
            Interlocked.Exchange(ref _pendingPollTaskCompletionSource, null)?.TrySetResult(true);
    }

    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Temporary tracing")]
    private ConsumeResult<byte[]?, byte[]?>? Consume(IConfluentConsumerWrapper client, CancellationToken cancellationToken)
    {
        try
        {
            return client.Consume(_consumer.Configuration.PollingTimeout);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Ignore exceptions if the client is being disconnected
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    private bool CheckConsumerStall()
    {
        if (!_consumer.Configuration.StallDetectionThreshold.HasValue ||
            DateTime.UtcNow - _lastSuccessfulConsume <= _consumer.Configuration.StallDetectionThreshold)
        {
            return true;
        }

        _logger.LogStaleConsumer(_consumer.Configuration.StallDetectionThreshold.Value, _consumer);
        _consumer.TriggerReconnectAsync().FireAndForget();

        return false;
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
