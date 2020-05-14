// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Util
{
    /// <summary>
    ///     Registered as Singleton holds the actual items list shared between the scoped instances of the
    ///     <see cref="TransactionalDictionary{TKey,TValue}" />.
    /// </summary>
    /// <typeparam name="TKey"> The type of the keys in the dictionary. </typeparam>
    /// <typeparam name="TValue"> The type of the values in the dictionary. </typeparam>
    public sealed class TransactionalDictionarySharedItems<TKey, TValue>
    {
        /// <summary>
        ///     Gets the underlying <see cref="Dictionary{TKey,TValue}" /> containing the persisted items.
        /// </summary>
        public Dictionary<TKey, TValue> Items { get; } = new Dictionary<TKey, TValue>();
    }
}
