// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWorker" />
public class OutboxWorker : IOutboxWorker
{
    private readonly OutboxWorkerSettings _settings;

    private readonly IOutboxReader _outboxReader;

    private readonly IProducerCollection _producers;

    private readonly IProducerLogger<OutboxWorker> _logger;

    private readonly ConcurrentBag<OutboxMessage> _producedMessages = [];

    private int _pendingProduceOperations;

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

    private static ProducerEndpoint GetEndpoint(OutboxMessage outboxMessage, ProducerEndpointConfiguration configuration) =>
        configuration.Endpoint switch
        {
            IStaticProducerEndpointResolver staticEndpointProvider => staticEndpointProvider.GetEndpoint(configuration),
            IDynamicProducerEndpointResolver dynamicEndpointProvider => dynamicEndpointProvider.Deserialize(
                outboxMessage.Endpoint.DynamicEndpoint ?? throw new InvalidOperationException("SerializedEndpoint is null"),
                configuration),
            _ => throw new InvalidOperationException("The IEndpointProvider is neither an IStaticEndpointProvider nor an IDynamicEndpointProvider.")
        };

    private async Task<bool> TryProcessOutboxAsync(CancellationToken stoppingToken)
    {
        _logger.LogReadingMessagesFromOutbox(_settings.BatchSize);

        _producedMessages.Clear();
        _failed = false;
        IReadOnlyCollection<OutboxMessage> outboxMessages = await _outboxReader.GetAsync(_settings.BatchSize).ConfigureAwait(false);

        if (outboxMessages.Count == 0)
        {
            _logger.LogOutboxEmpty();
            return false;
        }

        try
        {
            int index = 0;
            foreach (OutboxMessage outboxMessage in outboxMessages)
            {
                _logger.LogProcessingOutboxStoredMessage(++index, outboxMessages.Count);

                if (_settings.EnforceMessageOrder)
                    await BlockingProcessMessageAsync(outboxMessage).ConfigureAwait(false);
                else
                    ProcessMessage(outboxMessage);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Break on failure if message order has to be preserved
                if (_failed && _settings.EnforceMessageOrder)
                    break;
            }
        }
        finally
        {
            await WaitAllAsync().ConfigureAwait(false);
            await AcknowledgeAllAsync().ConfigureAwait(false);
        }

        return true;
    }

    [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "Produce with callbacks is potentially faster")]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Produce with callbacks is potentially faster")]
    private void ProcessMessage(OutboxMessage message)
    {
        try
        {
            IProducer producer = GetProducer(message);
            ProducerEndpoint endpoint = GetEndpoint(message, producer.EndpointConfiguration);

            Interlocked.Increment(ref _pendingProduceOperations);

            if (_failed && _settings.EnforceMessageOrder)
            {
                Interlocked.Decrement(ref _pendingProduceOperations);
                return;
            }

            // TODO: Avoid closure allocations
            producer.RawProduce(
                endpoint,
                message.Content,
                message.Headers,
                _ =>
                {
                    _producedMessages.Add(message);
                    Interlocked.Decrement(ref _pendingProduceOperations);
                },
                exception =>
                {
                    _failed = true;
                    Interlocked.Decrement(ref _pendingProduceOperations);

                    _logger.LogErrorProducingOutboxStoredMessage(
                        new OutboundEnvelope(message.Content, message.Headers, endpoint, producer),
                        exception);
                });
        }
        catch (Exception ex)
        {
            _failed = true;
            Interlocked.Decrement(ref _pendingProduceOperations);

            _logger.LogErrorProducingOutboxStoredMessage(ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
    }

    private async ValueTask BlockingProcessMessageAsync(OutboxMessage message)
    {
        try
        {
            IProducer producer = GetProducer(message);
            ProducerEndpoint endpoint = GetEndpoint(message, producer.EndpointConfiguration);

            await producer.RawProduceAsync(endpoint, message.Content, message.Headers).ConfigureAwait(false);
            _producedMessages.Add(message);
        }
        catch (Exception ex)
        {
            _failed = true;

            _logger.LogErrorProducingOutboxStoredMessage(ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
    }

    private IProducer GetProducer(OutboxMessage outboxMessage) => _producers.GetProducerForEndpoint(outboxMessage.Endpoint.FriendlyName);

    private Task AcknowledgeAllAsync() => _outboxReader.AcknowledgeAsync(_producedMessages);

    private async Task WaitAllAsync()
    {
        while (_pendingProduceOperations > 0)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }
    }
}
