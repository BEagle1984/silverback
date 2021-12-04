// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Util;

namespace Silverback.Collections;

/// <inheritdoc cref="IValueReadOnlyCollection{T}" />
public class ValueReadOnlyCollection<T> : IValueReadOnlyCollection<T>, IEquatable<ValueReadOnlyCollection<T>>
{
    private readonly IEqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

    private readonly IReadOnlyCollection<T> _collection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueReadOnlyCollection{T}" /> class.
    /// </summary>
    /// <param name="collection">
    ///     The collection whose elements are copied to the new <see cref="ValueReadOnlyCollection{T}" />.
    /// </param>
    public ValueReadOnlyCollection(IEnumerable<T> collection)
    {
        _collection = Check.NotNull(collection, nameof(collection)).AsArray();
    }

    /// <summary>
    ///     Gets a static instance of the <see cref="ValueReadOnlyCollection{T}" /> wrapping an empty array.
    /// </summary>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "OK to have an instance per actual type")]
    public static ValueReadOnlyCollection<T> Empty { get; } = new(Array.Empty<T>());

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _collection.Count;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(ValueReadOnlyCollection<T>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        using IEnumerator<T> enumerator1 = GetEnumerator();
        using IEnumerator<T> enumerator2 = other.GetEnumerator();

        while (enumerator1.MoveNext())
        {
            if (!enumerator2.MoveNext() || !_equalityComparer.Equals(enumerator1.Current, enumerator2.Current))
                return false;
        }

        return !enumerator2.MoveNext();
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) =>
        obj is { } && (ReferenceEquals(this, obj) || obj is ValueReadOnlyCollection<T> coll && Equals(coll));

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() =>
        unchecked(_collection.Aggregate(
            0,
            (current, element) => (current * 397) ^ (element == null ? 0 : _equalityComparer.GetHashCode(element))));

    /// <inheritdoc cref="IEnumerable.GetEnumerator" />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
