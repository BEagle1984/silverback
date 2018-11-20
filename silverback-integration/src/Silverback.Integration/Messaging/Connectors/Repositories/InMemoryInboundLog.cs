using System;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLog : TransactionalList<InMemoryInboundLogEntry>, IInboundLog
    {
        public void Add(IIntegrationMessage message, IEndpoint endpoint)
            => Add(new InMemoryInboundLogEntry(message.Id, endpoint.Name));

        public bool Exists(IIntegrationMessage message, IEndpoint endpoint)
            => Entries.Any(e => e.MessageId == message.Id && e.EndpointName == endpoint.Name);
    }
}