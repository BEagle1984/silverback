// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util
{
    /// <summary> Wraps an item stored in the <see cref="TransactionalList{T}" />. </summary>
    /// <typeparam name="T"> The type of the wrapped item. </typeparam>
    public class TransactionalListItem<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionalListItem{T}" /> class.
        /// </summary>
        /// <param name="item"> The actual item to be wrapped. </param>
        public TransactionalListItem(T item)
        {
            Item = item;
            InsertDate = DateTime.UtcNow;
        }

        /// <summary> Gets the actual item. </summary>
        public T Item { get; }

        /// <summary> Gets the datetime when the item was added to the list. </summary>
        public DateTime InsertDate { get; }
    }
}
