// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Background;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IBroker _broker;
        private readonly IBackgroundTaskManager _backgroundTaskManager;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<OutboundQueueWorker> _logger;

        private readonly int _readPackageSize;
        private readonly bool _enforceMessageOrder;

        private bool _running;

        public OutboundQueueWorker(IServiceProvider serviceProvider, IBroker broker, IBackgroundTaskManager backgroundTaskManager, ILogger<OutboundQueueWorker> logger,
            MessageLogger messageLogger, bool enforceMessageOrder, int readPackageSize)
        {
            _serviceProvider = serviceProvider;
            _broker = broker;
            _backgroundTaskManager = backgroundTaskManager;
            _messageLogger = messageLogger;
            _logger = logger;
            _enforceMessageOrder = enforceMessageOrder;
            _readPackageSize = readPackageSize;
        }

        public void ProcessQueue()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                ProcessQueue(scope.ServiceProvider.GetRequiredService<IOutboundQueueConsumer>());
            }
        }

        public void StartProcessing(CancellationToken cancellationToken = default, TimeSpan? interval = null, DistributedLockSettings lockSettings = null)
        {
            _logger.LogTrace("Starting OutboundQueueWorker...");

            cancellationToken.Register(WaitUntilStopped);

            var queue = _serviceProvider.GetRequiredService<IOutboundQueueConsumer>();

            _backgroundTaskManager.Start($"OutboundQueueWorker[{queue.GetType().FullName}]", () =>
            {
                _running = true;

                _logger.LogInformation("OutboundQueueWorker processing started.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    TryProcessQueue();

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    Sleep(interval);
                }

                _logger.LogInformation("OutboundQueueWorker processing stopped.");

                _running = false;
            }, lockSettings);
        }

        private void ProcessQueue(IOutboundQueueConsumer queue)
        {
            _logger.LogTrace($"Reading outbound messages from queue (limit: {_readPackageSize}).");

            var messages = queue.Dequeue(_readPackageSize).ToList();

            if (!messages.Any())
                _logger.LogTrace("The outbound queue is empty.");

            for (var i = 0; i < messages.Count; i++)
            {
                _logger.LogTrace($"Processing message {i + 1} of {messages.Count}.");
                ProcessMessage(messages[i], queue);
            }
        }

        private void TryProcessQueue()
        {
            try
            {
                ProcessQueue();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing the outbound queue. See inner exception for details.");
            }
        }

        private void Sleep(TimeSpan? interval)
        {
            var milliseconds = interval?.Milliseconds ?? 500;

            if (milliseconds <= 0)
                return;

            _logger.LogTrace($"Sleeping for {milliseconds} milliseconds.");

            Thread.Sleep(milliseconds);
        }

        private void ProcessMessage(QueuedMessage message, IOutboundQueueConsumer queue)
        {
            try
            {
                ProduceMessage(message.Message);

                queue.Acknowledge(message);
            }
            catch (Exception ex)
            {
                _messageLogger.LogError(_logger, ex, "Failed to publish queued message.", message?.Message.Message, message?.Message.Endpoint);

                queue.Retry(message);

                // Rethrow if message order has to be preserved, otherwise go ahead with next message in the queue
                if (_enforceMessageOrder)
                    throw;
            }
        }
        
        private void WaitUntilStopped()
        {
            while (_running)
            {
                Thread.Sleep(10);
            }
        }
        
        protected virtual void ProduceMessage(IOutboundMessage message)
            => _broker.GetProducer(message.Endpoint).Produce(message.Message, message.Headers);
    }
}
