// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLog : TransactionalList<InMemoryInboundLogEntry>, IInboundLog
    {
        private readonly MessageKeyProvider _messageKeyProvider;

        public InMemoryInboundLog(MessageKeyProvider messageKeyProvider)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        public void Add(object message, IEndpoint endpoint) =>
            Add(new InMemoryInboundLogEntry(_messageKeyProvider.GetKey(message), endpoint.Name));

        public bool Exists(object message, IEndpoint endpoint) =>
            Entries.Union(UncommittedEntries).Any(e =>
                e.MessageId == _messageKeyProvider.GetKey(message) && e.EndpointName == endpoint.Name);
    }
}