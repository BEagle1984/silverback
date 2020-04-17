// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class OutboundQueueWorker : IOutboundQueueWorker
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBrokerCollection _brokerCollection;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<OutboundQueueWorker> _logger;

        private readonly int _readPackageSize;
        private readonly bool _enforceMessageOrder;

        public OutboundQueueWorker(
            IServiceScopeFactory serviceScopeFactory,
            IBrokerCollection brokerCollection,
            ILogger<OutboundQueueWorker> logger,
            MessageLogger messageLogger,
            bool enforceMessageOrder,
            int readPackageSize)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _brokerCollection = brokerCollection;
            _messageLogger = messageLogger;
            _logger = logger;
            _enforceMessageOrder = enforceMessageOrder;
            _readPackageSize = readPackageSize;
        }

        public async Task ProcessQueue(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                await ProcessQueue(scope.ServiceProvider.GetRequiredService<IOutboundQueueConsumer>(), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing the outbound queue. See inner exception for details.");
            }
        }

        private async Task ProcessQueue(IOutboundQueueConsumer queue, CancellationToken stoppingToken)
        {
            _logger.LogTrace($"Reading outbound messages from queue (limit: {_readPackageSize}).");

            var messages = (await queue.Dequeue(_readPackageSize)).ToList();

            if (!messages.Any())
                _logger.LogTrace("The outbound queue is empty.");

            for (var i = 0; i < messages.Count; i++)
            {
                _logger.LogDebug($"Processing message {i + 1} of {messages.Count}.");
                await ProcessMessage(messages[i], queue);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }

        private async Task ProcessMessage(QueuedMessage message, IOutboundQueueConsumer queue)
        {
            try
            {
                await ProduceMessage(message.Content, message.Headers, message.Endpoint);

                await queue.Acknowledge(message);
            }
            catch (Exception ex)
            {
                _messageLogger.LogError(_logger, ex, "Failed to publish queued message.",
                    new OutboundEnvelope(message.Content, message.Headers, message.Endpoint));

                await queue.Retry(message);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }

        protected virtual Task ProduceMessage(
            byte[] content,
            IReadOnlyCollection<MessageHeader> headers,
            IProducerEndpoint endpoint)
            => _brokerCollection.GetProducer(endpoint).ProduceAsync(content, headers);
    }
}