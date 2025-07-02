// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
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

    private bool _failed;

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
        _settings = Check.NotNull(settings, nameof(settings));
        _outboxReader = Check.NotNull(outboxReader, nameof(outboxReader));
        _producers = Check.NotNull(producers, nameof(producers));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IOutboxWorker.ProcessOutboxAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async Task<bool> ProcessOutboxAsync(CancellationToken stoppingToken)
    {
        _logger.LogReadingMessagesFromOutbox(_settings.BatchSize);

        _pendingProduceCountdown.Reset();
        _producedMessages.Clear();
        _failed = false;

        using IDisposableAsyncEnumerable<OutboxMessage> outboxMessages = await _outboxReader.GetAsync(_settings.BatchSize).ConfigureAwait(false);

        try
        {
            int index = 0;
            await foreach (OutboxMessage outboxMessage in outboxMessages)
            {
                _logger.LogProcessingOutboxStoredMessage(++index);

                ProcessMessage(outboxMessage);

                stoppingToken.ThrowIfCancellationRequested();

                // Break on failure if message order has to be preserved
                if (_failed && _settings.EnforceMessageOrder)
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
        try
        {
            IProducer producer = GetProducer(outboxMessage);

            if (_failed && _settings.EnforceMessageOrder)
                return;

            _pendingProduceCountdown.AddCount();

            producer.RawProduce(
                outboxMessage.Content,
                outboxMessage.Headers,
                OnProduceSuccess,
                OnProduceError,
                outboxMessage);
        }
        catch (Exception ex)
        {
            _failed = true;
            _pendingProduceCountdown.Signal();

            _logger.LogErrorProducingOutboxStoredMessage(outboxMessage, ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
    }

    private void OnProduceSuccess(IBrokerMessageIdentifier? identifier, OutboxMessage outboxMessage)
    {
        _producedMessages.Add(outboxMessage);
        _pendingProduceCountdown.Signal();
    }

    private void OnProduceError(Exception exception, OutboxMessage outboxMessage)
    {
        _failed = true;
        _pendingProduceCountdown.Signal();

        _logger.LogErrorProducingOutboxStoredMessage(outboxMessage, exception);
    }

    private IProducer GetProducer(OutboxMessage outboxMessage) => _producers.GetProducerForEndpoint(outboxMessage.EndpointName);

    private Task AcknowledgeAllAsync() => _outboxReader.AcknowledgeAsync(_producedMessages);

    private Task WaitAllAsync() => _pendingProduceCountdown.WaitAsync();
}
