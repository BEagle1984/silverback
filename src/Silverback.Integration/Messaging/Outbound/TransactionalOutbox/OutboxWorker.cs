// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Messaging.Outbound.Deferred
{
    /// <inheritdoc cref="IOutboxWorker" />
    public class OutboxWorker : IOutboxWorker
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly IBrokerCollection _brokerCollection;

        private readonly IOutboundRoutingConfiguration _routingConfiguration;

        private readonly ISilverbackIntegrationLogger<OutboxWorker> _logger;

        private readonly int _readPackageSize;

        private readonly bool _enforceMessageOrder;

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
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     Specifies whether the messages must be produced in the same order as they were added to the queue.
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully
        ///     produced.
        /// </param>
        /// <param name="readPackageSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        public OutboxWorker(
            IServiceScopeFactory serviceScopeFactory,
            IBrokerCollection brokerCollection,
            IOutboundRoutingConfiguration routingConfiguration,
            ISilverbackIntegrationLogger<OutboxWorker> logger,
            bool enforceMessageOrder,
            int readPackageSize)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _brokerCollection = brokerCollection;
            _logger = logger;
            _enforceMessageOrder = enforceMessageOrder;
            _readPackageSize = readPackageSize;
            _routingConfiguration = routingConfiguration;
        }

        /// <inheritdoc cref="IOutboxWorker.ProcessQueueAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                await ProcessQueueAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    IntegrationEventIds.ErrorProcessingOutboundQueue,
                    ex,
                    "Error occurred processing the outbound queue.");
            }
        }

        /// <summary>
        ///     Gets the producer for the specified endpoint and produces the specified message.
        /// </summary>
        /// <param name="content">
        ///     The serialized message content (body).
        /// </param>
        /// <param name="headers">
        ///     The collection of message headers.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        protected virtual Task ProduceMessageAsync(
            byte[]? content,
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint) =>
            _brokerCollection.GetProducer(endpoint).ProduceAsync(content, headers);

        private async Task ProcessQueueAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            _logger.LogTrace(
                IntegrationEventIds.ReadingMessagesFromOutbox,
                "Reading outbound messages from queue (limit: {readPackageSize}).",
                _readPackageSize);

            var queue = serviceProvider.GetRequiredService<IOutboundQueueReader>();
            var messages = (await queue.Dequeue(_readPackageSize).ConfigureAwait(false)).ToList();

            if (messages.Count == 0)
                _logger.LogTrace(IntegrationEventIds.OutboxEmpty, "The outbound queue is empty.");

            for (var i = 0; i < messages.Count; i++)
            {
                _logger.LogDebug(
                    IntegrationEventIds.ProcessingEnqueuedOutboundMessage,
                    "Processing message {currentMessageIndex} of {totalMessages}.",
                    i + 1,
                    messages.Count);
                await ProcessMessageAsync(messages[i], queue, serviceProvider).ConfigureAwait(false);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }

        private async Task ProcessMessageAsync(
            QueuedMessage message,
            IOutboundQueueReader queue,
            IServiceProvider serviceProvider)
        {
            try
            {
                var endpoint = GetTargetEndpoint(message.MessageType, message.EndpointName, serviceProvider);
                await ProduceMessageAsync(message.Content, message.Headers, endpoint).ConfigureAwait(false);

                await queue.Acknowledge(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogErrorWithMessageInfo(
                    IntegrationEventIds.ErrorProducingEnqueuedMessage,
                    ex,
                    "Failed to produce the message in the outbox.",
                    new OutboundEnvelope(message.Content, message.Headers, new LoggingEndpoint(message.EndpointName)));

                await queue.Retry(message).ConfigureAwait(false);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }

        private IProducerEndpoint GetTargetEndpoint(
            Type? messageType,
            string endpointName,
            IServiceProvider serviceProvider)
        {
            var outboundRoutes = messageType != null
                ? _routingConfiguration.GetRoutesForMessage(messageType)
                : _routingConfiguration.Routes;

            var targetEndpoint = outboundRoutes
                .SelectMany(route => route.GetOutboundRouter(serviceProvider).Endpoints)
                .FirstOrDefault(endpoint => endpoint.Name == endpointName);

            if (targetEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"No endpoint with name '{endpointName}' could be found for a message " +
                    $"of type '{messageType?.FullName}'.");
            }

            return targetEndpoint;
        }

        private class LoggingEndpoint : ProducerEndpoint
        {
            public LoggingEndpoint(string name)
                : base(name)
            {
            }
        }
    }
}
