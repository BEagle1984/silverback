// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLog : TransactionalList<InMemoryInboundLogEntry>, IInboundLog
    {
        private readonly MessageIdProvider _messageIdProvider;

        public InMemoryInboundLog(MessageIdProvider messageIdProvider)
        {
            _messageIdProvider = messageIdProvider;
        }

        public Task Add(IRawInboundEnvelope envelope) =>
            Add(new InMemoryInboundLogEntry(_messageIdProvider.GetMessageId(envelope.Headers),
                envelope.Endpoint.GetUniqueConsumerGroupName()));

        public Task<bool> Exists(IRawInboundEnvelope envelope) =>
            Task.FromResult(Entries.Union(UncommittedEntries).Any(e =>
                e.MessageId == _messageIdProvider.GetMessageId(envelope.Headers) &&
                e.EndpointName == envelope.Endpoint.GetUniqueConsumerGroupName()));
    }
}