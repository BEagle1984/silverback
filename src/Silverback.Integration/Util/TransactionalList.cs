// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Util
{
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    public abstract class TransactionalList<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionalList{T}"/> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The items shared between the instances of this repository.
        /// </param>
        protected TransactionalList(TransactionalListSharedItems<T> sharedItems)
        {
            Items = sharedItems.Items;
            UncommittedItems = new List<TransactionalListItem<T>>();
        }

        protected List<TransactionalListItem<T>> Items { get; }
        protected List<TransactionalListItem<T>> UncommittedItems { get; }

        /// <summary>
        ///     Gets the number of items currently in the list, ignoring the uncommitted changes.
        /// </summary>
        public int CommittedItemsCount => Items.Count;

        protected Task Add(T entry)
        {
            lock (UncommittedItems)
            {
                UncommittedItems.Add(new TransactionalListItem<T>(entry));
            }

            return Task.CompletedTask;
        }

        public virtual Task Commit()
        {
            lock (UncommittedItems)
            {
                lock (Items)
                {
                    Items.AddRange(UncommittedItems);
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

        protected Task Remove(T entry)
        {
            lock (Items)
            {
                Items.RemoveAll(item => item.Entry.Equals(entry));
            }

            return Task.CompletedTask;
        }
    }
}