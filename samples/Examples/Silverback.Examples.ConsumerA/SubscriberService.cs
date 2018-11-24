using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
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
        void OnIntegrationEvent(IntegrationEvent message)
        {
            _logger.LogInformation($"Received IntegrationEvent '{message.Content}'");
        }

        [Subscribe]
        void OnBadEvent(BadIntegrationEvent message)
        {
            _logger.LogInformation($"Message '{message.Content}' is BAD...throwing exception!");
            throw new System.Exception("Bad message!");
        }

        [Subscribe]
        void OnLegacyMessageReceived(LegacyMessage message)
        {
            _logger.LogInformation($"Received legacy message '{message.Content}'");
        }
    }
}