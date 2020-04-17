// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Util
{
    /// <summary>
    ///     Registered as Singleton holds the actual items list shared between the scoped instances of the
    ///     <see cref="TransactionalList{T}" />.
    /// </summary>
    public sealed class TransactionalListSharedItems<T>
    {
        public List<TransactionalListItem<T>> Items = new List<TransactionalListItem<T>>();
    }
}