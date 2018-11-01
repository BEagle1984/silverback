using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
{
    public class QueuedMessage
    {
        public QueuedMessage(IIntegrationMessage message, IEndpoint endpoint)
        {
            Message = message;
            Endpoint = endpoint;
        }

        public IIntegrationMessage Message { get; }

        public IEndpoint Endpoint { get; }
    }
}
