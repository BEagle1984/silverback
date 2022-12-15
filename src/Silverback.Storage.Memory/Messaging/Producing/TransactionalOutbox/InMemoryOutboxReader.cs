// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Reads from the in-memory outbox.
/// </summary>
public class InMemoryOutboxReader : IOutboxReader
{
    private readonly InMemoryOutbox _outbox;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOutboxReader" /> class.
    /// </summary>
    /// <param name="outbox">
    ///     The in-memory outbox shared between the <see cref="InMemoryOutboxWriter" /> and <see cref="InMemoryOutboxReader" />.
    /// </param>
    public InMemoryOutboxReader(InMemoryOutbox outbox)
    {
        _outbox = Check.NotNull(outbox, nameof(outbox));
    }

    /// <inheritdoc cref="IOutboxReader.GetAsync" />
    public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => Task.FromResult(_outbox.Get(count));

    /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
    public Task<int> GetLengthAsync() => Task.FromResult(_outbox.ItemsCount);

    /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
    public Task<TimeSpan> GetMaxAgeAsync() => Task.FromResult(_outbox.GetMaxAge());

    /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
    public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));
        _outbox.Remove(outboxMessages);
        return Task.CompletedTask;
    }
}
