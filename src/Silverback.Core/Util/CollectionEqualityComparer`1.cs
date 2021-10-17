// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Util
{
    internal sealed class CollectionEqualityComparer<T> : CollectionEqualityComparer<T, T>
    {
        public CollectionEqualityComparer(bool enforceOrder = false)
            : base(item => item, enforceOrder)
        {
        }
    }
}
