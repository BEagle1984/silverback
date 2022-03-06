﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Database.Model;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

/// <summary>
///     <para>
///         Used by the <see cref="LogExactlyOnceStrategy" /> to keep track of each processed message and
///         guarantee that each one is processed only once.
///     </para>
///     <para>
///         The log is simply persisted in memory.
///     </para>
/// </summary>
public class InMemoryInboundLog : TransactionalList<InboundLogEntry>, IInboundLog
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryInboundLog" /> class.
    /// </summary>
    /// <param name="sharedItems">
    ///     The log entries shared between the instances of this repository.
    /// </param>
    public InMemoryInboundLog(TransactionalListSharedItems<InboundLogEntry> sharedItems)
        : base(sharedItems)
    {
    }

    /// <inheritdoc cref="IInboundLog.AddAsync" />
    public Task AddAsync(IRawInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
        string consumerGroupName = envelope.Endpoint.Configuration.GetUniqueConsumerGroupName();

        InboundLogEntry logEntry = new()
        {
            MessageId = messageId,
            EndpointName = envelope.Endpoint.RawName,
            ConsumerGroupName = consumerGroupName
        };

        return AddAsync(logEntry);
    }

    /// <inheritdoc cref="IInboundLog.ExistsAsync" />
    public Task<bool> ExistsAsync(IRawInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        string messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId, true)!;
        string consumerGroupName = envelope.Endpoint.Configuration.GetUniqueConsumerGroupName();

        return Task.FromResult(
            Items.Union(UncommittedItems).Any(
                item =>
                    item.Item.MessageId == messageId &&
                    item.Item.EndpointName == envelope.Endpoint.RawName &&
                    item.Item.ConsumerGroupName == consumerGroupName));
    }

    /// <inheritdoc cref="IInboundLog.GetLengthAsync" />
    public Task<int> GetLengthAsync() => Task.FromResult(CommittedItemsCount);
}
