// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Util
{
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public abstract class TransactionalDictionary<TKey, TValue>
    {
        protected TransactionalDictionary(TransactionalDictionarySharedItems<TKey, TValue> sharedItems)
        {
            Items = sharedItems.Items;
            UncommittedItems = new Dictionary<TKey, TValue>();
        }

        protected Dictionary<TKey, TValue> Items { get; }
        protected Dictionary<TKey, TValue> UncommittedItems { get; }

        /// <summary>
        ///     Gets the number of items currently in the dictionary, ignoring the uncommitted changes.
        /// </summary>
        public int CommittedItemsCount => Items.Count;

        protected Task AddOrReplace(TKey key, TValue value)
        {
            lock (UncommittedItems)
            {
                UncommittedItems[key] = value;
            }

            return Task.CompletedTask;
        }

        public virtual Task Commit()
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

        public virtual Task Rollback()
        {
            lock (UncommittedItems)
            {
                UncommittedItems.Clear();
            }

            return Task.CompletedTask;
        }

        protected Task Remove(TKey key)
        {
            lock (Items)
            {
                Items.Remove(key);
            }

            return Task.CompletedTask;
        }
    }
}