using System;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLog : TransactionalList<InboundLogEntry>, IInboundLog
    {
        public void Add(IIntegrationMessage message, IEndpoint endpoint)
            => Add(new InboundLogEntry(message.Id, endpoint.Name));

        public bool Exists(IIntegrationMessage message, IEndpoint endpoint)
            => Entries.Any(e => e.MessageId == message.Id && e.EndpointName == endpoint.Name);

        public void ClearOlderEntries(DateTime threshold)
        {
            lock (Entries)
            {
                Entries.RemoveAll(e => e.TimeStamp < threshold);
            }
        }
    }
}