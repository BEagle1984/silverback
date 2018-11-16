using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public interface IOutboundQueueWriter
    {
        Task Enqueue(IIntegrationMessage message, IEndpoint endpoint);

        Task Commit();

        Task Rollback();
    }
}