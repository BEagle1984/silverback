using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Util;

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

        private const int DequeuePackageSize = 100; // TODO: Parameter?
        private const bool PreserveOrdering = true; // TODO: Parameter?

        public OutboundQueueWorker(IOutboundQueueConsumer queue, IBroker broker, ILogger<OutboundQueueWorker> logger)
        {
            _queue = queue;
            _broker = broker;
            _logger = logger;
        }

        public void ProcessQueue()
        {
            foreach (var message in _queue.Dequeue(DequeuePackageSize))
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
                if (PreserveOrdering)
                    throw;
            }
        }

        protected virtual void ProduceMessage(IIntegrationMessage message, IEndpoint endpoint)
            => _broker.GetProducer(endpoint).Produce(Envelope.Create(message));
    }
}
