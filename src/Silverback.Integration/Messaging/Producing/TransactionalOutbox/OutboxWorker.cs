// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWorker" />
public sealed class OutboxWorker : IOutboxWorker, IDisposable
{
    private readonly OutboxWorkerSettings _settings;

    private readonly IOutboxReader _outboxReader;

    private readonly IProducerCollection _producers;

    private readonly ISilverbackLogger<OutboxWorker> _logger;

    private readonly ConcurrentBag<OutboxMessage> _producedMessages = [];

    private readonly DynamicCountdownEvent _pendingProduceCountdown = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorker" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The worker settings.
    /// </param>
    /// <param name="outboxReader">
    ///     The <see cref="IOutboxReader" /> to be used to retrieve the pending messages.
    /// </param>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public OutboxWorker(
        OutboxWorkerSettings settings,
        IOutboxReader outboxReader,
        IProducerCollection producers,
        ISilverbackLogger<OutboxWorker> logger)
    {
        _settings = Check.NotNull(settings);
        _outboxReader = Check.NotNull(outboxReader);
        _producers = Check.NotNull(producers);
        _logger = Check.NotNull(logger);
    }

    /// <summary>
    ///     Gets a value indicating whether the last <see cref="ProcessOutboxAsync" /> pass failed to produce some messages.
    /// </summary>
    internal bool LastPassFailed { get; private set; }

    /// <summary>
    ///     Gets the number of messages successfully produced in the last <see cref="ProcessOutboxAsync" /> pass.
    /// </summary>
    internal int LastPassProducedCount { get; private set; }

    /// <inheritdoc cref="IOutboxWorker.ProcessOutboxAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async Task<bool> ProcessOutboxAsync(CancellationToken stoppingToken)
    {
        _logger.LogReadingMessagesFromOutbox(_settings.BatchSize);

        _pendingProduceCountdown.Reset();
        _producedMessages.Clear();
        LastPassFailed = false;
        LastPassProducedCount = 0;

        using IDisposableAsyncEnumerable<OutboxMessage> outboxMessages = await _outboxReader.GetAsync(_settings.BatchSize).ConfigureAwait(false);

        try
        {
            int index = 0;
            await foreach (OutboxMessage outboxMessage in outboxMessages.ConfigureAwait(false))
            {
                _logger.LogProcessingOutboxStoredMessage(++index);

                ProcessMessage(outboxMessage);

                stoppingToken.ThrowIfCancellationRequested();

                // Break on failure if message order has to be preserved
                if (LastPassFailed && _settings.EnforceMessageOrder)
                    throw new OutboxProcessingException("Failed to produce message, aborting the outbox processing.");
            }

            if (index == 0)
            {
                _logger.LogOutboxEmpty();
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OutboxProcessingException("Failed to process the outbox.", ex);
        }
        finally
        {
            await WaitAllAsync().ConfigureAwait(false); // Stopping token not forwarded to prevent inconsistencies
            LastPassProducedCount = _producedMessages.Count;
            await AcknowledgeAllAsync().ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc cref="IOutboxWorker.GetLengthAsync" />
    public Task<int> GetLengthAsync() => _outboxReader.GetLengthAsync();

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _pendingProduceCountdown.Dispose();

    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Produce with callbacks is potentially faster")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Produce with callbacks is potentially faster")]
    private void ProcessMessage(OutboxMessage outboxMessage)
    {
        bool countAdded = false;
        Activity? previousActivity = Activity.Current;
        Activity? activity = null;

        try
        {
            IProducer producer = GetProducer(outboxMessage);

            if (LastPassFailed && _settings.EnforceMessageOrder)
                return;

            _pendingProduceCountdown.AddCount();
            countAdded = true;

            // The pipeline is bypassed, so the activity must be started here to continue the trace of the stored message. It is stopped
            // in the callbacks, after the producer logged the outcome, so that it can still be set as current while logging.
            MessageHeaderCollection headers = [.. outboxMessage.Headers ?? []];
            activity = Diagnostics.ActivitySources.StartProduceActivity(headers, producer.EndpointConfiguration.RawName);

            producer.RawProduce(
                outboxMessage.Content,
                headers,
                OnProduceSuccess,
                OnProduceError,
                (OutboxMessage: outboxMessage, Activity: activity));
        }
        catch (Exception ex)
        {
            // No callback is invoked if the produce throws
            activity?.Stop();

            LastPassFailed = true;

            if (countAdded)
                _pendingProduceCountdown.Signal();

            _logger.LogErrorProducingOutboxStoredMessage(outboxMessage, ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
        finally
        {
            // The activity is still running, but it must not be the parent of the next message's activity (this method is synchronous,
            // so the change to the execution context would otherwise flow back to the caller)
            Diagnostics.ActivitySources.RestoreCurrent(previousActivity);
        }
    }

    private void OnProduceSuccess(IBrokerMessageIdentifier? identifier, (OutboxMessage OutboxMessage, Activity Activity) state)
    {
        Diagnostics.ActivitySources.StopPreservingCurrent(state.Activity);

        _producedMessages.Add(state.OutboxMessage);
        _pendingProduceCountdown.Signal();
    }

    private void OnProduceError(Exception exception, (OutboxMessage OutboxMessage, Activity Activity) state)
    {
        Diagnostics.ActivitySources.StopPreservingCurrent(state.Activity);

        LastPassFailed = true;
        _pendingProduceCountdown.Signal();

        _logger.LogErrorProducingOutboxStoredMessage(state.OutboxMessage, exception);
    }

    private IProducer GetProducer(OutboxMessage outboxMessage) => _producers.GetProducerForEndpoint(outboxMessage.EndpointName);

    private Task AcknowledgeAllAsync() => _outboxReader.AcknowledgeAsync(_producedMessages);

    private Task WaitAllAsync() => _pendingProduceCountdown.WaitAsync();
}
