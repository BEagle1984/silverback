// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Tests
{
    public static class CollectionExtensions
    {
        public static void ThreadSafeAdd<T>(this ICollection<T> collection, T item)
        {
            lock (collection)
            {
                collection.Add(item);
            }
        }
    }
}
