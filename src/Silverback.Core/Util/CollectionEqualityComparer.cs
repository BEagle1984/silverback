// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Util
{
    internal static class CollectionEqualityComparer
    {
        public static CollectionEqualityComparer<string> String { get; } = new();

        public static CollectionEqualityComparer<byte> Byte { get; } = new(true);
    }
}
