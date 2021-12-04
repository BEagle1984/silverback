// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util;

internal class CollectionEqualityComparer<T, TKey> : IEqualityComparer<ICollection<T>>
{
    private readonly Func<T, TKey?> _keySelector;

    private readonly bool _enforceOrder;

    public CollectionEqualityComparer(Func<T, TKey?> keySelector, bool enforceOrder = false)
    {
        _keySelector = keySelector;
        _enforceOrder = enforceOrder;
    }

    public bool Equals(ICollection<T>? x, ICollection<T>? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(x, null))
            return y == null || y.Count == 0;
        if (ReferenceEquals(y, null))
            return x.Count == 0;

        return _enforceOrder
            ? x.SequenceEqual(y)
            : x.OrderBy(_keySelector).SequenceEqual(y.OrderBy(_keySelector));
    }

    public int GetHashCode(ICollection<T> obj) => obj.GetHashCode();
}
