// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Util;

/// <summary>
///     Registered as Singleton holds the actual items list shared between the scoped instances of the
///     <see cref="TransactionalList{T}" />.
/// </summary>
/// <typeparam name="T">
///     The type of the item in the list.
/// </typeparam>
public sealed class TransactionalListSharedItems<T>
{
    /// <summary>
    ///     Gets the underlying <see cref="List{T}" /> containing the persisted items.
    /// </summary>
    public IList<TransactionalListItem<T>> Items { get; } = new List<TransactionalListItem<T>>();
}
