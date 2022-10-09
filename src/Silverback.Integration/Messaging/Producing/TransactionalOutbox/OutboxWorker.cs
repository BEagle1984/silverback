﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly OutboxWorkerSettings _settings;

    private readonly IOutboxReader _outboxReader;

    private readonly IProducerCollection _producers;

    private readonly IProducerLogger<OutboxWorker> _logger;

    private readonly ConcurrentBag<OutboxMessage> _failedMessages = new();

    private readonly Action<IBrokerMessageIdentifier?> _onSuccess;

    private IReadOnlyCollection<OutboxMessage> _outboxMessages = Array.Empty<OutboxMessage>();

    private int _pendingProduceOperations;

    private IServiceScope? _serviceScope;

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
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public OutboxWorker(
        OutboxWorkerSettings settings,
        IOutboxReader outboxReader,
        IProducerCollection producers,
        IServiceScopeFactory serviceScopeFactory,
        IProducerLogger<OutboxWorker> logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _outboxReader = Check.NotNull(outboxReader, nameof(outboxReader));
        _producers = Check.NotNull(producers, nameof(producers));
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
        _logger = Check.NotNull(logger, nameof(logger));

        _onSuccess = _ => Interlocked.Decrement(ref _pendingProduceOperations);
    }

    /// <inheritdoc cref="IOutboxWorker.ProcessOutboxAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
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
        finally
        {
            _serviceScope?.Dispose();
        }
    }

    /// <inheritdoc cref="IOutboxWorker.GetLengthAsync" />
    public Task<int> GetLengthAsync() => _outboxReader.GetLengthAsync();

    private async Task<bool> TryProcessOutboxAsync(CancellationToken stoppingToken)
    {
        _logger.LogReadingMessagesFromOutbox(_settings.BatchSize);

        _failedMessages.Clear();
        _outboxMessages = await _outboxReader.GetAsync(_settings.BatchSize).ConfigureAwait(false);

        if (_outboxMessages.Count == 0)
        {
            _logger.LogOutboxEmpty();
            return false;
        }

        try
        {
            int index = 0;
            foreach (OutboxMessage outboxMessage in _outboxMessages)
            {
                _logger.LogProcessingOutboxStoredMessage(++index, _outboxMessages.Count);

                await ProcessMessageAsync(outboxMessage).ConfigureAwait(false);

                if (stoppingToken.IsCancellationRequested)
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

    private async Task ProcessMessageAsync(OutboxMessage message)
    {
        try
        {
            IProducer producer = GetProducer(message);
            ProducerEndpoint endpoint = await GetEndpointAsync(message, producer.EndpointConfiguration).ConfigureAwait(false);

            Interlocked.Increment(ref _pendingProduceOperations);

            // TODO: Avoid closure allocations
            await producer.RawProduceAsync(
                    endpoint,
                    message.Content,
                    message.Headers,
                    _onSuccess,
                    exception =>
                    {
                        _failedMessages.Add(message);
                        Interlocked.Decrement(ref _pendingProduceOperations);

                        _logger.LogErrorProducingOutboxStoredMessage(
                            new OutboundEnvelope(message.Content, message.Headers, endpoint, producer),
                            exception);
                    })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _failedMessages.Add(message);
            Interlocked.Decrement(ref _pendingProduceOperations);

            _logger.LogErrorProducingOutboxStoredMessage(ex);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_settings.EnforceMessageOrder)
                throw;
        }
    }

    // TODO: Test all cases
    private IProducer GetProducer(OutboxMessage outboxMessage)
    {
        IReadOnlyCollection<IProducer> producers = outboxMessage.MessageType != null
            ? _producers.GetProducersForMessage(outboxMessage.MessageType)
            : _producers;

        List<IProducer> matchingProducers = producers
            .Where(producer => producer.EndpointConfiguration.RawName == outboxMessage.Endpoint.RawName)
            .ToList();

        if (matchingProducers.Count == 0)
        {
            throw new InvalidOperationException(
                $"No endpoint with name '{outboxMessage.Endpoint.RawName}' could be found for a message " +
                $"of type '{outboxMessage.MessageType?.FullName}'.");
        }

        if (matchingProducers.Count > 1)
        {
            IProducer? matchingProducer = matchingProducers.FirstOrDefault(
                producer =>
                    producer.EndpointConfiguration.FriendlyName == outboxMessage.Endpoint.FriendlyName);

            if (matchingProducer != null)
                return matchingProducer;
        }

        return matchingProducers[0];
    }

    // TODO: Test all cases
    private async ValueTask<ProducerEndpoint> GetEndpointAsync(OutboxMessage outboxMessage, ProducerEndpointConfiguration configuration)
    {
        switch (configuration.Endpoint)
        {
            case IStaticProducerEndpointResolver staticEndpointProvider:
                return staticEndpointProvider.GetEndpoint(configuration);
            case IDynamicProducerEndpointResolver dynamicEndpointProvider when outboxMessage.Endpoint.SerializedEndpoint == null:
                _serviceScope ??= _serviceScopeFactory.CreateScope();
                return dynamicEndpointProvider.GetEndpoint(outboxMessage.Content, configuration, _serviceScope.ServiceProvider);
            case IDynamicProducerEndpointResolver dynamicEndpointProvider:
                return await dynamicEndpointProvider.DeserializeAsync(outboxMessage.Endpoint.SerializedEndpoint, configuration).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The IEndpointProvider is neither an IStaticEndpointProvider nor an IDynamicEndpointProvider.");
    }

    private Task AcknowledgeAllAsync() =>
        _outboxReader.AcknowledgeAsync(_outboxMessages.Where(message => !_failedMessages.Contains(message)));

    private async Task WaitAllAsync()
    {
        while (_pendingProduceOperations > 0)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}