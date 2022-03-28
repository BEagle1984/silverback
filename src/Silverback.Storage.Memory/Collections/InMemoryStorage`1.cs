// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Util;

namespace Silverback.Collections;

/// <summary>
///     This class is used as in-memory storage for various items.
/// </summary>
/// <typeparam name="T">
///     The type of the entities.
/// </typeparam>
public class InMemoryStorage<T>
    where T : class
{
    private readonly List<InMemoryStoredItem<T>> _items = new();

    /// <summary>
    ///     Gets the number of items in the storage.
    /// </summary>
    public int ItemsCount
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
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
        lock (_items)
        {
            if (_items.Count == 0)
                return TimeSpan.Zero;

            DateTime oldestInsertDateTime = _items.Min(item => item.InsertDateTime);

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
    public IReadOnlyCollection<T> Get(int count)
    {
        lock (_items)
        {
            return _items.Take(count).Select(item => item.Item).ToList();
        }
    }

    /// <summary>
    ///     Adds the specified item to the storage.
    /// </summary>
    /// <param name="item">
    ///     The item to add.
    /// </param>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "_items is locked when used inside SinglePhaseNotification")]
    public void Add(T item)
    {
        InMemoryStoredItem<T> inMemoryStoredItem = new(item);

        lock (_items)
        {
            _items.Add(inMemoryStoredItem);
        }
    }

    /// <summary>
    ///     Removes the specified items from the storage.
    /// </summary>
    /// <param name="itemsToRemove">
    ///     The items to remove.
    /// </param>
    public void Remove(IEnumerable<T> itemsToRemove)
    {
        Check.NotNull(itemsToRemove, nameof(itemsToRemove));

        lock (_items)
        {
            foreach (T itemToRemove in itemsToRemove)
            {
                int index = _items.FindIndex(item => item.Item == itemToRemove);

                if (index >= 0)
                    _items.RemoveAt(index);
            }
        }
    }
}
