// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Examples.Common.Subscribers
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class SampleEventsSubscriber : ISubscriber
    {
        private readonly ILogger<SampleEventsSubscriber> _logger;

        public SampleEventsSubscriber(ILogger<SampleEventsSubscriber> logger)
        {
            _logger = logger;
        }

        [Subscribe(Parallel = true)]
        public void OnIntegrationEventReceived(IntegrationEvent message)
        {
            _logger.LogInformation($"Received IntegrationEvent '{message.Content}'");
        }

        [Subscribe(Parallel = true)]
        public void OnIntegrationEventBatchReceived(IEnumerable<IntegrationEvent> messages)
        {
            if (messages.Count() <= 1) return;

            _logger.LogInformation($"Received batch containing {messages.Count()} IntegrationEvent messages.");
        }

        [Subscribe(Parallel = true)]
        public void OnIntegrationEventReceived(IObservable<IntegrationEvent> messages) =>
            messages.Subscribe(message =>
            {
                _logger.LogInformation($"Observed IntegrationEvent '{message.Content}'");
            });

        public async Task OnBadEventReceived(BadIntegrationEvent message)
        {
            _logger.LogInformation($"Message '{message.Content}' is BAD...throwing exception!");

            await DoFail();
        }

        public void OnEmptyMessageReceived(IInboundEnvelope envelope)
        {
            if (envelope.Message == null)
                _logger.LogInformation("Empty message received!");
        }

        private Task DoFail()
        {
            throw new AggregateException(new Exception("Bad message!", new Exception("Inner reason...")));
        }
    }
}