// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     An <see cref="IOutboxWriter" /> that produces the messages to the message broker right away, instead of storing them, in the same
///     way the <see cref="IOutboxWorker" /> would do.
/// </summary>
internal sealed class BypassOutboxWriter : IOutboxWriter
{
    private readonly IProducerCollection _producers;

    public BypassOutboxWriter(IProducerCollection producers)
    {
        _producers = Check.NotNull(producers, nameof(producers));
    }

    public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
        ProduceAsync(Check.NotNull(outboxMessage, nameof(outboxMessage)), cancellationToken);

    public async Task AddAsync(
        IEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            await ProduceAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddAsync(
        IAsyncEnumerable<OutboxMessage> outboxMessages,
        ISilverbackContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        await foreach (OutboxMessage outboxMessage in outboxMessages.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await ProduceAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProduceAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        IProducer producer = _producers.GetProducerForEndpoint(outboxMessage.EndpointName);

        // The headers already include the serialized endpoint, if any, exactly like when they are read back from the outbox
        MessageHeaderCollection headers = [.. outboxMessage.Headers ?? []];

        await producer.RawProduceAsync(outboxMessage.Content, headers, cancellationToken).ConfigureAwait(false);
    }
}
