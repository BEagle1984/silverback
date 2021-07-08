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
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
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
                using var scope = _serviceScopeFactory.CreateScope();
                await ProcessQueueAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogErrorProcessingOutbox(ex);
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
        /// <param name="actualEndpointName">
        ///     The actual endpoint name that was resolved for the message.
        /// </param>
        /// <param name="onSuccess">
        ///     The callback to be invoked when the message is successfully produced.
        /// </param>
        /// <param name="onError">
        ///     The callback to be invoked when the produce fails.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task ProduceMessageAsync(
            byte[]? content,
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            _brokerCollection.GetProducer(endpoint).RawProduceAsync(
                actualEndpointName,
                content,
                headers,
                onSuccess,
                onError);

        private static async Task AcknowledgeAllAsync(
            IOutboxReader outboxReader,
            List<OutboxStoredMessage> messages,
            ConcurrentBag<OutboxStoredMessage> failedMessages)
        {
            await outboxReader.RetryAsync(failedMessages).ConfigureAwait(false);

            await outboxReader.AcknowledgeAsync(messages.Where(message => !failedMessages.Contains(message)))
                .ConfigureAwait(false);
        }

        private async Task ProcessQueueAsync(
            IServiceProvider serviceProvider,
            CancellationToken stoppingToken)
        {
            _logger.LogReadingMessagesFromOutbox(_batchSize);

            var failedMessages = new ConcurrentBag<OutboxStoredMessage>();

            var outboxReader = serviceProvider.GetRequiredService<IOutboxReader>();
            var outboxMessages =
                (await outboxReader.ReadAsync(_batchSize).ConfigureAwait(false)).ToList();

            if (outboxMessages.Count == 0)
            {
                _logger.LogOutboxEmpty();
                return;
            }

            try
            {
                Interlocked.Add(ref _pendingProduceOperations, outboxMessages.Count);

                for (var i = 0; i < outboxMessages.Count; i++)
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
                var endpoint = GetTargetEndpoint(message.MessageType, message.EndpointName, serviceProvider);

                await ProduceMessageAsync(
                        message.Content,
                        message.Headers,
                        endpoint,
                        message.ActualEndpointName ?? endpoint.Name,
                        _ =>
                        {
                            Interlocked.Decrement(ref _pendingProduceOperations);
                        },
                        exception =>
                        {
                            failedMessages.Add(message);
                            Interlocked.Decrement(ref _pendingProduceOperations);

                            _logger.LogErrorProducingOutboxStoredMessage(
                                new OutboundEnvelope(
                                    message.Content,
                                    message.Headers,
                                    new LoggingEndpoint(message.EndpointName)),
                                exception);
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failedMessages.Add(message);
                Interlocked.Decrement(ref _pendingProduceOperations);

                _logger.LogErrorProducingOutboxStoredMessage(
                    new OutboundEnvelope(
                        message.Content,
                        message.Headers,
                        new LoggingEndpoint(message.EndpointName)),
                    ex);

                await outboxReader.RetryAsync(message).ConfigureAwait(false);

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
                .FirstOrDefault(
                    endpoint => endpoint.Name == endpointName || endpoint.DisplayName == endpointName);

            if (targetEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"No endpoint with name '{endpointName}' could be found for a message " +
                    $"of type '{messageType?.FullName}'.");
            }

            return targetEndpoint;
        }

        private async Task WaitAllAsync()
        {
            while (_pendingProduceOperations > 0)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
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
