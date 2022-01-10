// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     Reads from the in-memory outbox.
/// </summary>
public class InMemoryOutboxReader : IOutboxReader
{
    private readonly InMemoryStorage<OutboxMessage> _storage;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxReader" /> class.
    /// </summary>
    /// <param name="storage">
    ///     The in-memory storage shared between the <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" />.
    /// </param>
    public InMemoryOutboxReader(InMemoryStorage<OutboxMessage> storage)
    {
        _storage = Check.NotNull(storage, nameof(storage));
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => Task.FromResult(_storage.Get(count));

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public Task<int> GetLengthAsync() => Task.FromResult(_storage.ItemsCount);

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(_storage.GetMaxAge());

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));
        _storage.Remove(outboxMessages);
        return Task.CompletedTask;
    }
}
