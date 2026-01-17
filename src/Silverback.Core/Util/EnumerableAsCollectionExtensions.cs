// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Util;

internal static class EnumerableAsCollectionExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public IReadOnlyCollection<T> AsReadOnlyCollection() =>
            enumerable as IReadOnlyCollection<T> ?? [.. enumerable];

        public IReadOnlyList<T> AsReadOnlyList() =>
            enumerable as IReadOnlyList<T> ?? [.. enumerable];

        public List<T> AsList() =>
            enumerable as List<T> ?? [.. enumerable];

        public T[] AsArray() =>
            enumerable as T[] ?? [.. enumerable];
    }
}
