// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     Writes to the in-memory outbox.
/// </summary>
public class InMemoryOutboxWriter : IOutboxWriter
{
    private readonly InMemoryStorage<OutboxMessage> _storage;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxWriter" /> class.
    /// </summary>
    /// <param name="storage">
    ///     The in-memory storage shared between the <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" />.
    /// </param>
    public InMemoryOutboxWriter(InMemoryStorage<OutboxMessage> storage)
    {
        _storage = Check.NotNull(storage, nameof(storage));
    }

    /// <inheritdoc cref="AddAsync" />
    public Task AddAsync(OutboxMessage outboxMessage)
    {
        _storage.Add(outboxMessage);
        return Task.CompletedTask;
    }
}
