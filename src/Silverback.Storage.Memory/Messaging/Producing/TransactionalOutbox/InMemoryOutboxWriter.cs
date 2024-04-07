// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Writes to the in-memory outbox.
/// </summary>
public class InMemoryOutboxWriter : IOutboxWriter
{
    private readonly InMemoryOutbox _outbox;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxWriter" /> class.
    /// </summary>
    /// <param name="outbox">
    ///     The in-memory outbox shared between the <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" />.
    /// </param>
    public InMemoryOutboxWriter(InMemoryOutbox outbox)
    {
        _outbox = Check.NotNull(outbox, nameof(outbox));
    }

    /// <inheritdoc cref="AddAsync(OutboxMessage, SilverbackContext)" />
    public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null)
    {
        WarnIfTransaction(context);

        _outbox.Add(outboxMessage);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.SilverbackContext?)" />
    public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        WarnIfTransaction(context);

        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            _outbox.Add(outboxMessage);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="AddAsync(System.Collections.Generic.IAsyncEnumerable{Silverback.Messaging.Producing.TransactionalOutbox.OutboxMessage},Silverback.SilverbackContext?)" />
    public async Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        WarnIfTransaction(context);

        await foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            _outbox.Add(outboxMessage);
        }
    }

    private static void WarnIfTransaction(SilverbackContext? context)
    {
        if (context != null && context.TryGetStorageTransaction(out _))
        {
            // TODO: Log warning
        }
    }
}
