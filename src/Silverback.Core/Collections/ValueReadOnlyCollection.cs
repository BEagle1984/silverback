// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Collections;

/// <summary>
///     Provides the <see cref="Empty{T}"/> factory method.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Named after the actual collection")]
public static class ValueReadOnlyCollection
{
    /// <summary>
    ///     Returns an empty <see cref="ValueReadOnlyCollection{T}" />.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the elements.
    /// </typeparam>
    /// <returns>
    ///     The empty <see cref="ValueReadOnlyCollection{T}" />.
    /// </returns>
    public static ValueReadOnlyCollection<T> Empty<T>() => EmptyValueReadOnlyCollection<T>.Instance;

    internal class EmptyValueReadOnlyCollection<T>
    {
        public static readonly ValueReadOnlyCollection<T> Instance = new(Array.Empty<T>());
    }
}
