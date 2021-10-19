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
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="IOutboxWorker" />
public class OutboxWorker : IOutboxWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly IBrokerCollection _brokerCollection;

    private readonly IOutboundRoutingConfiguration _routingConfiguration;

    private readonly IOutboundLogger<OutboxWorker> _logger;

    private readonly int _batchSize;

    private readonly bool _enforceMessageOrder;

    private int _pendingProduceOperations;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorker" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" /> used to resolve the scoped types.
    /// </param>
    /// <param name="brokerCollection">
    ///     The collection containing the available brokers.
    /// </param>
    /// <param name="routingConfiguration">
    ///     The configured outbound routes.
    /// </param>
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
        IServiceScopeFactory serviceScopeFactory,
        IBrokerCollection brokerCollection,
        IOutboundRoutingConfiguration routingConfiguration,
        IOutboundLogger<OutboxWorker> logger,
        bool enforceMessageOrder,
        int batchSize)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _brokerCollection = brokerCollection;
        _logger = logger;
        _enforceMessageOrder = enforceMessageOrder;
        _batchSize = batchSize;
        _routingConfiguration = routingConfiguration;
    }

    /// <inheritdoc cref="IOutboxWorker.ProcessQueueAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    public async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope? scope = _serviceScopeFactory.CreateScope();
            await ProcessQueueAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogErrorProcessingOutbox(ex);
        }
    }

    private static async Task AcknowledgeAllAsync(
        IOutboxReader outboxReader,
        List<OutboxStoredMessage> messages,
        ConcurrentBag<OutboxStoredMessage> failedMessages)
    {
        await outboxReader.RetryAsync(failedMessages).ConfigureAwait(false);

        await outboxReader.AcknowledgeAsync(messages.Where(message => !failedMessages.Contains(message)))
            .ConfigureAwait(false);
    }

    private static async Task<ProducerEndpoint> GetEndpointAsync(
        OutboxStoredMessage message,
        ProducerConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        switch (configuration.Endpoint)
        {
            // TODO: Test all cases


            case IStaticProducerEndpointResolver staticEndpointProvider:
                return staticEndpointProvider.GetEndpoint(configuration);
            case IDynamicProducerEndpointResolver dynamicEndpointProvider when message.Endpoint == null:
                return dynamicEndpointProvider.GetEndpoint(message.Content, configuration, serviceProvider);
            case IDynamicProducerEndpointResolver dynamicEndpointProvider:
                return await dynamicEndpointProvider.DeserializeAsync(message.Endpoint, configuration).ConfigureAwait(false);
        }

        throw new InvalidOperationException("The IEndpointProvider is neither an IStaticEndpointProvider nor an IDynamicEndpointProvider.");
    }

    private async Task ProcessQueueAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        _logger.LogReadingMessagesFromOutbox(_batchSize);

        ConcurrentBag<OutboxStoredMessage> failedMessages = new();

        IOutboxReader? outboxReader = serviceProvider.GetRequiredService<IOutboxReader>();
        List<OutboxStoredMessage> outboxMessages = (await outboxReader.ReadAsync(_batchSize).ConfigureAwait(false)).ToList();

        if (outboxMessages.Count == 0)
        {
            _logger.LogOutboxEmpty();
            return;
        }

        try
        {
            Interlocked.Add(ref _pendingProduceOperations, outboxMessages.Count);

            for (int i = 0; i < outboxMessages.Count; i++)
            {
                _logger.LogProcessingOutboxStoredMessage(i + 1, outboxMessages.Count);

                await ProcessMessageAsync(
                        outboxMessages[i],
                        failedMessages,
                        outboxReader,
                        serviceProvider)
                    .ConfigureAwait(false);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }
        finally
        {
            await WaitAllAsync().ConfigureAwait(false);
            await AcknowledgeAllAsync(outboxReader, outboxMessages, failedMessages).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxStoredMessage message,
        ConcurrentBag<OutboxStoredMessage> failedMessages,
        IOutboxReader outboxReader,
        IServiceProvider serviceProvider)
    {
        try
        {
            ProducerEndpoint actualEndpoint = await GetEndpointAsync(
                    message,
                    GetProducerSettings(message.MessageType, message.EndpointRawName, message.EndpointFriendlyName),
                    serviceProvider)
                .ConfigureAwait(false);


            // TODO: Avoid closure allocations


            await _brokerCollection.GetProducer(
                    GetProducerSettings(
                        message.MessageType,
                        message.EndpointRawName,
                        message.EndpointFriendlyName))
                .RawProduceAsync(
                    actualEndpoint,
                    message.Content,
                    message.Headers,
                    _ => Interlocked.Decrement(ref _pendingProduceOperations),
                    exception =>
                    {
                        failedMessages.Add(message);
                        Interlocked.Decrement(ref _pendingProduceOperations);

                        _logger.LogErrorProducingOutboxStoredMessage(
                            new OutboundEnvelope(
                                message.Content,
                                message.Headers,
                                actualEndpoint),
                            exception);
                    })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failedMessages.Add(message);
            Interlocked.Decrement(ref _pendingProduceOperations);

            _logger.LogErrorProducingOutboxStoredMessage(ex);

            await outboxReader.RetryAsync(message).ConfigureAwait(false);

            // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
            if (_enforceMessageOrder)
                throw;
        }
    }

    private ProducerConfiguration GetProducerSettings(Type? messageType, string rawName, string? friendlyName)
    {
        // TODO: Test all cases


        IReadOnlyCollection<IOutboundRoute> outboundRoutes = messageType != null
            ? _routingConfiguration.GetRoutesForMessage(messageType)
            : _routingConfiguration.Routes;

        List<ProducerConfiguration> matchingRawNameEndpoints = outboundRoutes
            .Select(route => route.ProducerConfiguration)
            .Where(endpoint => endpoint.RawName == rawName)
            .ToList();

        if (matchingRawNameEndpoints.Count == 0)
        {
            throw new InvalidOperationException(
                $"No endpoint with name '{rawName}' could be found for a message " +
                $"of type '{messageType?.FullName}'.");
        }

        if (matchingRawNameEndpoints.Count > 1)
        {
            ProducerConfiguration? matchingFriendlyNameEndpoint =
                matchingRawNameEndpoints.FirstOrDefault(endpoint => endpoint.FriendlyName == friendlyName);

            if (matchingFriendlyNameEndpoint != null)
                return matchingFriendlyNameEndpoint;
        }

        return matchingRawNameEndpoints[0];
    }

    private async Task WaitAllAsync()
    {
        while (_pendingProduceOperations > 0)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
