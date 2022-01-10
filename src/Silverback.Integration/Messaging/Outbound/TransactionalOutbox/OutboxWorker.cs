// Copyright (c) 2020 Sergio Aquilini
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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWorker" />
public class OutboxWorker : IOutboxWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly OutboxWorkerSettings _settings;

    private readonly IOutboxReader _outboxReader;

    private readonly IBrokerCollection _brokerCollection;

    private readonly IOutboundRoutingConfiguration _routingConfiguration;

    private readonly IOutboundLogger<OutboxWorker> _logger;

    private readonly ConcurrentBag<OutboxMessage> _failedMessages = new();

    private readonly Action<IBrokerMessageIdentifier?> _onSuccess;

    private IReadOnlyCollection<OutboxMessage> _outboxMessages;

    private int _pendingProduceOperations;

    private IServiceScope? _serviceScope;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorker" /> class.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="outboxReader"></param>
    /// <param name="brokerCollection">
    ///     The collection containing the available brokers.
    /// </param>
    /// <param name="routingConfiguration">
    ///     The configured outbound routes.
    /// </param>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="logger">
    ///     The <see cref="IOutboundLogger{TCategoryName}" />.
    /// </param>
    /// <param name="enforceMessageOrder">
    ///     Specifies whether the messages must be produced in the same order as they were added to the queue.
    ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
    ///     successfully
    ///     produced.
    /// </param>
    /// <param name="batchSize">
    ///     The number of messages to be loaded and processed at once.
    /// </param>
    public OutboxWorker(
        OutboxWorkerSettings settings,
        IOutboxReader outboxReader,
        IBrokerCollection brokerCollection,
        IOutboundRoutingConfiguration routingConfiguration,
        IServiceScopeFactory serviceScopeFactory,
        IOutboundLogger<OutboxWorker> logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _outboxReader = Check.NotNull(outboxReader, nameof(outboxReader));
        _brokerCollection = Check.NotNull(brokerCollection, nameof(brokerCollection));
        _routingConfiguration = Check.NotNull(routingConfiguration, nameof(routingConfiguration));
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
            ProducerConfiguration producerConfiguration = GetProducerConfiguration(message);
            ProducerEndpoint endpoint = await GetEndpointAsync(message, producerConfiguration).ConfigureAwait(false);

            Interlocked.Increment(ref _pendingProduceOperations);

            // TODO: Avoid closure allocations
            IProducer producer = await _brokerCollection.GetProducerAsync(producerConfiguration).ConfigureAwait(false);
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
                            new OutboundEnvelope(
                                message.Content,
                                message.Headers,
                                endpoint),
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
    private ProducerConfiguration GetProducerConfiguration(OutboxMessage outboxMessage)
    {
        IReadOnlyCollection<IOutboundRoute> outboundRoutes = outboxMessage.MessageType != null
            ? _routingConfiguration.GetRoutesForMessage(outboxMessage.MessageType)
            : _routingConfiguration.Routes;

        List<ProducerConfiguration> matchingRawNameEndpoints = outboundRoutes
            .Select(route => route.ProducerConfiguration)
            .Where(configuration => configuration.RawName == outboxMessage.Endpoint.RawName)
            .ToList();

        if (matchingRawNameEndpoints.Count == 0)
        {
            throw new InvalidOperationException(
                $"No endpoint with name '{outboxMessage.Endpoint.RawName}' could be found for a message " +
                $"of type '{outboxMessage.MessageType?.FullName}'.");
        }

        if (matchingRawNameEndpoints.Count > 1)
        {
            ProducerConfiguration? matchingFriendlyNameEndpoint =
                matchingRawNameEndpoints.FirstOrDefault(configuration => configuration.FriendlyName == outboxMessage.Endpoint.FriendlyName);

            if (matchingFriendlyNameEndpoint != null)
                return matchingFriendlyNameEndpoint;
        }

        return matchingRawNameEndpoints[0];
    }

    // TODO: Test all cases
    private async ValueTask<ProducerEndpoint> GetEndpointAsync(OutboxMessage outboxMessage, ProducerConfiguration configuration)
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
