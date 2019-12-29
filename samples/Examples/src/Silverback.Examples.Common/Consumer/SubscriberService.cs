// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Common.Consumer
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public class SubscriberService : ISubscriber
    {
        private readonly ILogger<SubscriberService> _logger;

        public SubscriberService(ILogger<SubscriberService> logger)
        {
            _logger = logger;
        }

        [Subscribe(Parallel = true)]
        private void OnIntegrationEventReceived(IntegrationEvent message)
        {
            _logger.LogInformation($"Received IntegrationEvent '{message.Content}'");
        }

        [Subscribe(Parallel = true)]
        private void OnIntegrationEventBatchReceived(IEnumerable<IntegrationEvent> messages)
        {
            if (messages.Count() <= 1) return;

            _logger.LogInformation($"Received batch containing {messages.Count()} IntegrationEvent messages.");
        }

        [Subscribe(Parallel = true)]
        private void OnIntegrationEventReceived(IObservable<IntegrationEvent> messages) =>
            messages.Subscribe(message =>
            {
                _logger.LogInformation($"Observed IntegrationEvent '{message.Content}'");
            });

        [Subscribe]
        private async Task OnBadEventReceived(BadIntegrationEvent message)
        {
            _logger.LogInformation($"Message '{message.Content}' is BAD...throwing exception!");

            await DoFail();
        }

        private Task DoFail()
        {
            throw new AggregateException(new Exception("Bad message!", new Exception("Inner reason...")));
        }

        [Subscribe]
        private void OnLegacyMessageReceived(LegacyMessage message)
        {
            _logger.LogInformation($"Received legacy message '{message.Content}'");
        }

        [Subscribe]
        private void OnBatchComplete(BatchCompleteEvent message)
        {
            _logger.LogInformation($"Batch '{message.BatchId} ready ({message.BatchSize} messages)");
        }

        [Subscribe]
        private void OnBatchProcessed(BatchProcessedEvent message)
        {
            _logger.LogInformation($"Successfully processed batch '{message.BatchId} ({message.BatchSize} messages)");
        }

        [Subscribe]
        private void OnMessageMoved(MessageMovedEvent @event)
        {
            foreach (var id in @event.Identifiers)
            {
                _logger.LogInformation($"MessageMovedEvent :: Message '{id}' moved from '{@event.Source}' to '{@event.Destination}'");
            }
        }
    }
}