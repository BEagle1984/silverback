// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Silverback.Util;

namespace Silverback.Collections;

/// <inheritdoc cref="IValueReadOnlyCollection{T}" />
public sealed class ValueReadOnlyCollection<T> : IValueReadOnlyCollection<T>, IEquatable<ValueReadOnlyCollection<T>>
{
    private readonly EqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

    private readonly IReadOnlyCollection<T> _collection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueReadOnlyCollection{T}" /> class.
    /// </summary>
    /// <param name="collection">
    ///     The collection whose elements are copied to the new <see cref="ValueReadOnlyCollection{T}" />.
    /// </param>
    public ValueReadOnlyCollection(IEnumerable<T> collection)
    {
        _collection = Check.NotNull(collection, nameof(collection)).AsReadOnlyCollection();
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _collection.Count;

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    [MustDisposeResource]
    public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

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

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) =>
        obj is { } && (ReferenceEquals(this, obj) || obj is ValueReadOnlyCollection<T> coll && Equals(coll));

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() =>
        unchecked(_collection.Aggregate(
            0,
            (current, element) => (current * 397) ^ (element == null ? 0 : _equalityComparer.GetHashCode(element))));

    /// <inheritdoc cref="IEnumerable.GetEnumerator" />
    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
