// Copyright (c) 2018-2019 Sergio Aquilini
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
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<OutboundQueueWorker> _logger;

        private readonly int _readPackageSize;
        private readonly bool _enforceMessageOrder;

        public OutboundQueueWorker(IOutboundQueueConsumer queue, IBroker broker, ILogger<OutboundQueueWorker> logger,
            MessageLogger messageLogger, bool enforceMessageOrder, int readPackageSize)
        {
            _queue = queue;
            _broker = broker;
            _messageLogger = messageLogger;
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
                ProduceMessage(message.Message);

                _queue.Acknowledge(message);
            }
            catch (Exception ex)
            {
                _messageLogger.LogError(_logger, ex, "Failed to publish queued message.", message?.Message.Message, message?.Message.Endpoint);

                _queue.Retry(message);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }

        protected virtual void ProduceMessage(IOutboundMessage message)
            => _broker.GetProducer(message.Endpoint).Produce(message.Message, message.Headers);
    }
}
