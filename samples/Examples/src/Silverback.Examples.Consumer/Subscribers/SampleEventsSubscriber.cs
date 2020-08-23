// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Subscriber")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Subscriber")]
    public class SampleEventsSubscriber
    {
        private readonly ExamplesDbContext _dbContext;

        private readonly ILogger<SampleEventsSubscriber> _logger;

        public SampleEventsSubscriber(ILogger<SampleEventsSubscriber> logger, ExamplesDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [Subscribe]
        public async Task OnIntegrationEventReceived(IntegrationEvent message)
        {
            _logger.LogInformation("Received IntegrationEvent {@message}", message);

            if (!(message is BadIntegrationEvent))
                await _dbContext.SaveChangesAsync();
        }

        [Subscribe(Parallel = true)]
        public void OnIntegrationEventBatchReceived(IReadOnlyCollection<IntegrationEvent> messages)
        {
            if (messages.Count <= 1)
                return;

            _logger.LogInformation($"Received batch containing {messages.Count} IntegrationEvent messages");
        }

        [Subscribe(Parallel = true)]
        public void OnIntegrationEventReceived(IObservable<IntegrationEvent> messages) =>
            messages.Subscribe(message => { _logger.LogInformation("Observed IntegrationEvent {@message}", message); });

        public async Task OnBadEventReceived(BadIntegrationEvent message)
        {
            if (message.TryCount > 3)
            {
                _logger.LogInformation("Message {@message} can be finally processed.", message);
                return;
            }

            _logger.LogInformation("Message {@message} is BAD...throwing exception!", message);
            message.TryCount++;

            await DoFail();
        }

        public void OnEmptyMessageReceived(IInboundEnvelope envelope)
        {
            if (envelope.Message == null)
                _logger.LogInformation("Empty message received!");
        }

        public void OnAvroMessageReceived(AvroMessage message)
        {
            _logger.LogInformation("Received AvroMessage {@message}", message);
        }

        private static Task DoFail()
        {
            throw new AggregateException(
                new InvalidOperationException(
                    "Bad message!",
                    new InvalidOperationException("Inner reason...")));
        }
    }
}
