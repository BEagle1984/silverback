// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    /// <inheritdoc cref="AddAsync" />
    public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null)
    {
        if (context != null && context.TryGetStorageTransaction(out _))
        {
            // TODO: Log warning
        }

        _outbox.Add(outboxMessage);
        return Task.CompletedTask;
    }
}
