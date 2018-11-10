using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Integration.Repositories
{
    public interface IOutboundQueueWriter
    {
        void Enqueue(IIntegrationMessage message, IEndpoint endpoint);

        void Commit();

        void Rollback();
    }
}