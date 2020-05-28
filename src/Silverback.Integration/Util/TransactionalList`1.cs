// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util
{
    /// <summary>
    ///     Wraps the changes being made to the underlying <see cref="List{T}" /> into a transaction.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the items in the list.
    /// </typeparam>
    public abstract class TransactionalList<T>
        where T : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionalList{T}" /> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The items shared between the instances of this repository.
        /// </param>
        protected TransactionalList(TransactionalListSharedItems<T> sharedItems)
        {
            Check.NotNull(sharedItems, nameof(sharedItems));

            Items = sharedItems.Items;
            UncommittedItems = new List<TransactionalListItem<T>>();
        }

        /// <summary>
        ///     Gets the number of items currently in the list, ignoring the uncommitted changes.
        /// </summary>
        public int CommittedItemsCount => Items.Count;

        /// <summary>
        ///     Gets the underlying <see cref="List{T}" /> containing the persisted items, wrapped into a
        ///     <see cref="TransactionalListItem{T}" />.
        /// </summary>
        protected List<TransactionalListItem<T>> Items { get; }

        /// <summary>
        ///     Gets the <see cref="List{T}" /> containing the pending items that will be persisted when <c>
        ///         Commit
        ///     </c> is called.
        /// </summary>
        protected List<TransactionalListItem<T>> UncommittedItems { get; }

        /// <summary>
        ///     Called to commit the pending changes.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
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

        /// <summary>
        ///     Called to rollback the pending changes.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public virtual Task Rollback()
        {
            lock (UncommittedItems)
            {
                UncommittedItems.Clear();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Adds the specified item to the list.
        /// </summary>
        /// <param name="item">
        ///     The item to be added.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected Task Add(T item)
        {
            lock (UncommittedItems)
            {
                UncommittedItems.Add(new TransactionalListItem<T>(item));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Removes the specified item to the list.
        /// </summary>
        /// <param name="item">
        ///     The item to be added.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected Task Remove(T item)
        {
            lock (Items)
            {
                Items.RemoveAll(storedItem => storedItem.Item.Equals(item));
            }

            return Task.CompletedTask;
        }
    }
}
