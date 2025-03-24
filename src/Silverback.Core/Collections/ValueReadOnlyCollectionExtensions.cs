// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Collections;

/// <summary>
///     Adds the <see cref="AsValueReadOnlyCollection{T}" /> method to the <see cref="IReadOnlyCollection{T}" />.
/// </summary>
public static class ValueReadOnlyCollectionExtensions
{
    /// <summary>
    ///     Returns an <see cref="IValueReadOnlyCollection{T}" /> containing the elements copied from the specified
    ///     <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <param name="collection">
    ///     The collection whose elements are copied to the new <see cref="IValueReadOnlyCollection{T}" />.
    /// </param>
    /// <typeparam name="T">
    ///     The type of the elements.
    /// </typeparam>
    /// <returns>
    ///     An <see cref="IValueReadOnlyCollection{T}" /> that wraps the <see cref="IReadOnlyCollection{T}" />.
    /// </returns>
    public static IValueReadOnlyCollection<T> AsValueReadOnlyCollection<T>(this IEnumerable<T> collection) =>
        collection as IValueReadOnlyCollection<T> ?? new ValueReadOnlyCollection<T>(collection);
}
