// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLog : TransactionalList<InMemoryInboundLogEntry>, IInboundLog
    {
        private readonly MessageIdProvider _messageIdProvider;

        public InMemoryInboundLog(
            MessageIdProvider messageIdProvider,
            TransactionalListSharedItems<InMemoryInboundLogEntry> sharedItems)
            : base(sharedItems)
        {
            _messageIdProvider = messageIdProvider;
        }

        public Task Add(IRawInboundEnvelope envelope) =>
            Add(new InMemoryInboundLogEntry(_messageIdProvider.GetMessageId(envelope.Headers),
                envelope.Endpoint.GetUniqueConsumerGroupName()));

        public Task<bool> Exists(IRawInboundEnvelope envelope) =>
            Task.FromResult(Items.Union(UncommittedItems).Any(item =>
                item.Entry.MessageId == _messageIdProvider.GetMessageId(envelope.Headers) &&
                item.Entry.EndpointName == envelope.Endpoint.GetUniqueConsumerGroupName()));

        public Task<int> GetLength() => Task.FromResult(CommittedItemsCount);
    }
}