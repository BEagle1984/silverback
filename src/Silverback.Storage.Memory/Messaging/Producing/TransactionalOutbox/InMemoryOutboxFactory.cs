// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The in-memory outbox factory.
/// </summary>
public sealed class InMemoryOutboxFactory
{
    private readonly ConcurrentDictionary<OutboxSettings, InMemoryOutbox> _outboxes = new();

    /// <summary>
    ///     Gets the outbox matching the specified settings.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryOutbox" />.
    /// </returns>
    public InMemoryOutbox GetOutbox(OutboxSettings settings) =>
        _outboxes.GetOrAdd(settings, _ => new InMemoryOutbox());
}
