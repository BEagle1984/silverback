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
        void OnSimpleEvent(SimpleIntegrationEvent message)
        {
            _logger.LogInformation($"Received message '{message.Content}'");
        }

        [Subscribe]
        void OnBadEvent(BadIntegrationEvent message)
        {
            _logger.LogInformation($"Received message '{message.Content}'...throwing exception!");
            throw new System.Exception("Bad message!");
        }

        [Subscribe]
        void OnCustomSerializedEvent(CustomSerializedIntegrationEvent message)
        {
            _logger.LogInformation($"Received message '{message.Content}'");
        }

        [Subscribe]
        void OnLegacyMessageReceived(LegacyMessage message)
        {
            _logger.LogInformation($"Received legacy message '{message.Content}'");
        }
    }
}