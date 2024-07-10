// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Silverback.Tests;

/// <summary>
///     Used to ensure that the <see cref="IEnumerable{T}" /> is not implementing other interfaces like <see cref="IReadOnlyCollection{T}" />.
/// </summary>
public static class PureEnumerableExtensions
{
    public static IEnumerable<T> ToPureEnumerable<T>(this IEnumerable<T> enumerable) => new PureEnumerable<T>(enumerable);

    private class PureEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;

        public PureEnumerable(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        [MustDisposeResource]
        public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

        [MustDisposeResource]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
