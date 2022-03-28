// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Collections;

/// <summary>
///     Wraps the item stored in the <see cref="InMemoryStorage{T}" />.
/// </summary>
/// <typeparam name="T">
///     The type of the item.
/// </typeparam>
public class InMemoryStoredItem<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryStoredItem{T}" /> class.
    /// </summary>
    /// <param name="item">
    ///     The item to be wrapped.
    /// </param>
    /// <param name="insertDateTime">
    ///     The date and time when the item was inserted. When <c>null</c>, the current date and time is used.
    /// </param>
    public InMemoryStoredItem(T item, DateTime? insertDateTime = null)
    {
        Item = item;
        InsertDateTime = insertDateTime ?? DateTime.UtcNow;
    }

    /// <summary>
    ///     Gets the wrapped item.
    /// </summary>
    public T Item { get; }

    /// <summary>
    ///     Gets the date and time when the item was inserted in the list.
    /// </summary>
    public DateTime InsertDateTime { get; }
}
