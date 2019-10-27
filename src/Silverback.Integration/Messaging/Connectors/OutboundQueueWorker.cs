// Copyright (c) 2019 Sergio Aquilini
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IBroker _broker;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<OutboundQueueWorker> _logger;

        private readonly int _readPackageSize;
        private readonly bool _enforceMessageOrder;
        public OutboundQueueWorker(IServiceProvider serviceProvider, IBroker broker, ILogger<OutboundQueueWorker> logger,
            MessageLogger messageLogger, bool enforceMessageOrder, int readPackageSize)
        {
            _serviceProvider = serviceProvider;
            _broker = broker;
            _messageLogger = messageLogger;
            _logger = logger;
            _enforceMessageOrder = enforceMessageOrder;
            _readPackageSize = readPackageSize;
        }

        public async Task ProcessQueue(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
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
                _logger.LogTrace($"Processing message {i + 1} of {messages.Count}.");
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
                _messageLogger.LogError(_logger, ex, "Failed to publish queued message.", new OutboundMessage(message.Content, message.Headers, message.Endpoint));

                await queue.Retry(message);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }
        
        protected virtual Task ProduceMessage(byte[] content, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
            => _broker.GetProducer(endpoint).ProduceAsync(content, headers);
    }
}
