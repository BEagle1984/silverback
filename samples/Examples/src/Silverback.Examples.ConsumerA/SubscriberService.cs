// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class SubscriberService : ISubscriber
    {
        private readonly ILogger<SubscriberService> _logger;

        public SubscriberService(ILogger<SubscriberService> logger)
        {
            _logger = logger;
        }

        [Subscribe(Parallel = true)]
        void OnIntegrationEventReceived(IntegrationEvent message)
        {
            _logger.LogInformation($"Received IntegrationEvent '{message.Content}'");
        }

        [Subscribe(Parallel = true)]
        void OnIntegrationEventBatchReceived(IEnumerable<IntegrationEvent> messages)
        {
            if (messages.Count() <= 1) return;

            _logger.LogInformation($"Received batch containing {messages.Count()} IntegrationEvent messages.");
        }

        [Subscribe(Parallel = true)]
        void OnIntegrationEventReceived(IObservable<IntegrationEvent> messages) =>
            messages.Subscribe(message =>
            {
                _logger.LogInformation($"Observed IntegrationEvent '{message.Content}'");
            });

        [Subscribe]
        void OnBadEventReceived(BadIntegrationEvent message)
        {
            _logger.LogInformation($"Message '{message.Content}' is BAD...throwing exception!");
            throw new System.Exception("Bad message!");
        }

        [Subscribe]
        void OnLegacyMessageReceived(LegacyMessage message)
        {
            _logger.LogInformation($"Received legacy message '{message.Content}'");
        }

        [Subscribe]
        void OnBatchComplete(BatchCompleteEvent message)
        {
            _logger.LogInformation($"Batch '{message.BatchId} ready ({message.BatchSize} messages)");
        }

        [Subscribe]
        void OnBatchProcessed(BatchProcessedEvent message)
        {
            _logger.LogInformation($"Successfully processed batch '{message.BatchId} ({message.BatchSize} messages)");
        }

        [Subscribe]
        void OnMessageMoved(MessageMovedEvent @event)
        {
            _logger.LogInformation($"MessageMovedEvent :: Message '{@event.Id}' moved to '{@event.Destination}'");
        }
    }
}