using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
{
    public interface IOutboundQueueWriter
    {
        void Enqueue(IIntegrationMessage message, IEndpoint endpoint);

        void Commit();

        void Rollback();
    }
}