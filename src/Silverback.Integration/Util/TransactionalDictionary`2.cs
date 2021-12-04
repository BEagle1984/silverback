// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Util;

/// <summary>
///     Wraps the changes being made to the underlying <see cref="Dictionary{TKey,TValue}" /> into a
///     transaction.
/// </summary>
/// <typeparam name="TKey">
///     The type of the keys in the dictionary.
/// </typeparam>
/// <typeparam name="TValue">
///     The type of the values in the dictionary.
/// </typeparam>
[SuppressMessage("", "CA1711", Justification = "Dictionary suffix is appropriate.")]
public abstract class TransactionalDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TransactionalDictionary{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="sharedItems">
    ///     The dictionary items that are persisted and shared between the instances of this class.
    /// </param>
    protected TransactionalDictionary(TransactionalDictionarySharedItems<TKey, TValue> sharedItems)
    {
        Check.NotNull(sharedItems, nameof(sharedItems));

        Items = sharedItems.Items;
        UncommittedItems = new Dictionary<TKey, TValue>();
    }

    /// <summary>
    ///     Gets the number of items currently in the dictionary, ignoring the uncommitted changes.
    /// </summary>
    public int CommittedItemsCount => Items.Count;

    /// <summary>
    ///     Gets the underlying <see cref="Dictionary{TKey,TValue}" /> containing the persisted items.
    /// </summary>
    protected Dictionary<TKey, TValue> Items { get; }

    /// <summary>
    ///     Gets the <see cref="Dictionary{TKey,TValue}" /> containing the pending items that will be persisted when <see cref="CommitAsync" />
    ///     is called.
    /// </summary>
    protected Dictionary<TKey, TValue> UncommittedItems { get; }

    /// <summary>
    ///     Called to commit the pending changes.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public virtual Task CommitAsync()
    {
        lock (UncommittedItems)
        {
            lock (Items)
            {
                UncommittedItems.ForEach(item => Items[item.Key] = item.Value);
            }

            UncommittedItems.Clear();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called to rollback the pending changes.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public virtual Task RollbackAsync()
    {
        lock (UncommittedItems)
        {
            UncommittedItems.Clear();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds or replaces an item in the dictionary.
    /// </summary>
    /// <param name="key">
    ///     The item key.
    /// </param>
    /// <param name="value">
    ///     The item value.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected Task AddOrReplaceAsync(TKey key, TValue value)
    {
        lock (UncommittedItems)
        {
            UncommittedItems[key] = value;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Removes the item with the specified key.
    /// </summary>
    /// <param name="key">
    ///     The item key.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected Task RemoveAsync(TKey key)
    {
        lock (Items)
        {
            Items.Remove(key);
        }

        return Task.CompletedTask;
    }
}
