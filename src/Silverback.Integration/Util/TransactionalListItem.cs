// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util
{
    /// <summary>
    ///     An item stored in the <see cref="TransactionalList{T}" />.
    /// </summary>
    public class TransactionalListItem<T>
    {
        public TransactionalListItem(T entry)
        {
            Entry = entry;
            InsertDate = DateTime.UtcNow;
        }

        public T Entry { get; }

        public DateTime InsertDate { get; }
    }
}