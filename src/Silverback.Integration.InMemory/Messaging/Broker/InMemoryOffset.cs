// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IOffset" />
    public class InMemoryOffset : IComparableOffset
    {
        public InMemoryOffset(string key, int offset)
        {
            Key = key;
            Offset = offset;
            Value = offset.ToString();
        }

        public string Key { get; }

        public string Value { get; }

        public int Offset { get; }

        public string ToLogString() => Offset.ToString();

        public int CompareTo(InMemoryOffset other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Offset.CompareTo(other.Offset);
        }

        public int CompareTo(IOffset obj)
        {
            if (ReferenceEquals(this, obj)) return 0;
            if (obj is null) return 1;
            return obj is InMemoryOffset other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(InMemoryOffset)}");
        }

        public static bool operator <(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) < 0;

        public static bool operator >(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) > 0;

        public static bool operator <=(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) <= 0;

        public static bool operator >=(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) >= 0;
    }
}