// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     <para>
    ///         Used by the <see cref="LoggedInboundConnector" /> to keep track of each processed message and
    ///         guarantee that each one is processed only once.
    ///     </para>
    ///     <para> The log is simply persisted in memory. </para>
    /// </summary>
    public class InMemoryInboundLog : TransactionalList<InMemoryInboundLogEntry>, IInboundLog
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryInboundLog" /> class.
        /// </summary>
        /// <param name="sharedItems"> The log entries shared between the instances. </param>
        public InMemoryInboundLog(TransactionalListSharedItems<InMemoryInboundLogEntry> sharedItems)
            : base(sharedItems)
        {
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task Add(IRawInboundEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
            string consumerGroupName = envelope.Endpoint.GetUniqueConsumerGroupName();

            return Add(new InMemoryInboundLogEntry(messageId, consumerGroupName));
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task<bool> Exists(IRawInboundEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            return Task.FromResult(
                Items.Union(UncommittedItems).Any(
                    item =>
                    {
                        string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;

                        return item.Entry.MessageId == messageId &&
                               item.Entry.ConsumerGroupName == envelope.Endpoint.GetUniqueConsumerGroupName();
                    }));
        }

        /// <inheritdoc />
        public Task<int> GetLength() => Task.FromResult(CommittedItemsCount);
    }
}
