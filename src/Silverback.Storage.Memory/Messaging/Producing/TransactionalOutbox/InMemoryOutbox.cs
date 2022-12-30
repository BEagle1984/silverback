// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The in-memory outbox.
/// </summary>
public class InMemoryOutbox
{
    private readonly List<StoredOutboxMessage> _storedOutboxMessages = new();

    /// <summary>
    ///     Gets the number of items in the storage.
    /// </summary>
    public int ItemsCount
    {
        get
        {
            lock (_storedOutboxMessages)
            {
                return _storedOutboxMessages.Count;
            }
        }
    }

    /// <summary>
    ///     Gets a <see cref="TimeSpan" /> representing the time elapsed since the oldest message currently in the storage was written.
    /// </summary>
    /// <returns>
    ///     The <see cref="TimeSpan" /> representing the time elapsed since the oldest message currently in the storage was written.
    /// </returns>
    public TimeSpan GetMaxAge()
    {
        lock (_storedOutboxMessages)
        {
            if (_storedOutboxMessages.Count == 0)
                return TimeSpan.Zero;

            DateTime oldestInsertDateTime = _storedOutboxMessages.Min(item => item.InsertDateTime);

            return oldestInsertDateTime == default
                ? TimeSpan.Zero
                : DateTime.UtcNow - oldestInsertDateTime;
        }
    }

    /// <summary>
    ///     Gets the specified number of items from the storage, starting from the oldest.
    /// </summary>
    /// <param name="count">
    ///     Specifies the number of items to retrieve.
    /// </param>
    /// <returns>
    ///     The items.
    /// </returns>
    public IReadOnlyCollection<OutboxMessage> Get(int count)
    {
        lock (_storedOutboxMessages)
        {
            return _storedOutboxMessages.Take(count).Select(storedOutboxMessage => storedOutboxMessage.OutboxMessage).ToArray();
        }
    }

    /// <summary>
    ///     Adds the specified item to the storage.
    /// </summary>
    /// <param name="outboxMessage">
    ///     The message to add.
    /// </param>
    public void Add(OutboxMessage outboxMessage)
    {
        lock (_storedOutboxMessages)
        {
            _storedOutboxMessages.Add(new StoredOutboxMessage(outboxMessage));
        }
    }

    /// <summary>
    ///     Removes the specified items from the storage.
    /// </summary>
    /// <param name="outboxMessages">
    ///     The messages to remove.
    /// </param>
    public void Remove(IEnumerable<OutboxMessage> outboxMessages)
    {
        Check.NotNull(outboxMessages, nameof(outboxMessages));

        lock (_storedOutboxMessages)
        {
            foreach (OutboxMessage outboxMessage in outboxMessages)
            {
                int index = _storedOutboxMessages.FindIndex(storedOutboxMessage => storedOutboxMessage.OutboxMessage == outboxMessage);

                if (index >= 0)
                    _storedOutboxMessages.RemoveAt(index);
            }
        }
    }

    private sealed record StoredOutboxMessage(OutboxMessage OutboxMessage)
    {
        public DateTime InsertDateTime { get; } = DateTime.UtcNow;
    }
}
