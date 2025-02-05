// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWorker" />
public sealed class OutboxWorker : IOutboxWorker, IDisposable
{
    private readonly OutboxWorkerSettings _settings;

    private readonly IOutboxReader _outboxReader;

    private readonly IProducerCollection _producers;

    private readonly IProducerLogger<OutboxWorker> _logger;

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
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public OutboxWorker(
        OutboxWorkerSettings settings,
        IOutboxReader outboxReader,
        IProducerCollection producers,
        IProducerLogger<OutboxWorker> logger)
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
        try
        {
            return await TryProcessOutboxAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogErrorProcessingOutbox(ex);
            return false;
        }
    }

    /// <inheritdoc cref="IOutboxWorker.GetLengthAsync" />
    public Task<int> GetLengthAsync() => _outboxReader.GetLengthAsync();

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _pendingProduceCountdown.Dispose();

    private async Task<bool> TryProcessOutboxAsync(CancellationToken stoppingToken)
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

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Break on failure if message order has to be preserved
                if (_failed && _settings.EnforceMessageOrder)
                    break;
            }

            if (index == 0)
            {
                _logger.LogOutboxEmpty();
                return false;
            }
        }
        finally
        {
            await WaitAllAsync().ConfigureAwait(false); // Stopping token not forwarded to prevent inconsistencies
            await AcknowledgeAllAsync().ConfigureAwait(false);
        }

        return !_failed;
    }

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
                new ProduceState(producer, producer.EndpointConfiguration, outboxMessage));
        }
        catch (Exception ex)
        {
            _failed = true;
            _pendingProduceCountdown.Signal();

            _logger.LogErrorProducingOutboxStoredMessage(ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
    }

    private void OnProduceSuccess(IBrokerMessageIdentifier? identifier, ProduceState state)
    {
        _producedMessages.Add(state.OutboxMessage);
        _pendingProduceCountdown.Signal();
    }

    private void OnProduceError(Exception exception, ProduceState state)
    {
        _failed = true;
        _pendingProduceCountdown.Signal();

        _logger.LogErrorProducingOutboxStoredMessage(
            new OutboundEnvelope(state.OutboxMessage.Content, state.OutboxMessage.Headers, state.EndpointConfiguration, state.Producer),
            exception);
    }

    private IProducer GetProducer(OutboxMessage outboxMessage) => _producers.GetProducerForEndpoint(outboxMessage.EndpointName);

    private Task AcknowledgeAllAsync() => _outboxReader.AcknowledgeAsync(_producedMessages);

    private Task WaitAllAsync() => _pendingProduceCountdown.WaitAsync();

    internal record struct ProduceState(IProducer Producer, ProducerEndpointConfiguration EndpointConfiguration, OutboxMessage OutboxMessage);
}
