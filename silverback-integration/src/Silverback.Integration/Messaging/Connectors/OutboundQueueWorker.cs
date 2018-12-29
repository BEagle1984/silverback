// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Publishes the messages in the outbox queue to the configured message broker.
    /// </summary>
    public class OutboundQueueWorker
    {
        private readonly IOutboundQueueConsumer _queue;
        private readonly IBroker _broker;
        private readonly ILogger<OutboundQueueWorker> _logger;

        private readonly int _readPackageSize;
        private readonly bool _enforceMessageOrder;

        public OutboundQueueWorker(IOutboundQueueConsumer queue, IBroker broker, ILogger<OutboundQueueWorker> logger, bool enforceMessageOrder, int readPackageSize)
        {
            _queue = queue;
            _broker = broker;
            _logger = logger;
            _enforceMessageOrder = enforceMessageOrder;
            _readPackageSize = readPackageSize;
        }

        public void ProcessQueue()
        {
            foreach (var message in _queue.Dequeue(_readPackageSize))
            {
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(QueuedMessage message)
        {
            try
            {
                ProduceMessage(message.Message, message.Endpoint);

                _queue.Acknowledge(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Failed to publish queued message '{message?.Message?.Id}' to the endpoint '{message?.Endpoint?.Name}'.");

                _queue.Retry(message);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }

        protected virtual void ProduceMessage(IIntegrationMessage message, IEndpoint endpoint)
            => _broker.GetProducer(endpoint).Produce(message);
    }
}
