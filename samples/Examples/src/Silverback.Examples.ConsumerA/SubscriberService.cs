// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

        [Subscribe]
        void OnIntegrationEventReceived(IntegrationEvent message)
        {
            _logger.LogInformation($"Received IntegrationEvent '{message.Content}'");
        }

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
        void OnBatchReady(BatchReadyEvent message)
        {
            _logger.LogInformation($"Batch '{message.BatchId} ready ({message.BatchSize} messages)");
        }

        [Subscribe]
        void OnBatchProcessed(BatchProcessedEvent message)
        {
            _logger.LogInformation($"Successfully processed batch '{message.BatchId} ({message.BatchSize} messages)");
        }
    }
}