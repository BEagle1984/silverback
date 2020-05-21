// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IOffset" />
    public class InMemoryOffset : IComparableOffset
    {
        /// <summary> Initializes a new instance of the <see cref="InMemoryOffset" /> class. </summary>
        /// <param name="key"> The unique key of the queue, topic or partition this offset belongs to. </param>
        /// <param name="offset"> The offset value. </param>
        public InMemoryOffset(string key, int offset)
        {
            Key = key;
            Offset = offset;
            Value = offset.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public string Value { get; }

        /// <summary>
        ///     Gets the offset value. With the <see cref="InMemoryBroker" /> this will just be a basic sequence.
        /// </summary>
        public int Offset { get; }

        /// <summary> Less than operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator <(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) < 0;

        /// <summary> Greater than operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator >(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) > 0;

        /// <summary> Less than or equal operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator <=(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) <= 0;

        /// <summary> Greater than or equal operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator >=(InMemoryOffset left, InMemoryOffset right) =>
            Comparer<InMemoryOffset>.Default.Compare(left, right) >= 0;

        /// <summary> Equality operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator ==(InMemoryOffset left, InMemoryOffset right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator !=(InMemoryOffset left, InMemoryOffset right) => !(left == right);

        /// <inheritdoc />
        public string ToLogString() => Offset.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that
        ///     indicates whether the current instance precedes, follows, or occurs in the same position in the sort
        ///     order as the other object.
        /// </summary>
        /// <param name="other"> An object to compare with the current instance. </param>
        /// <returns>
        ///     A value less than zero if this is less than object, zero if this is equal to object, or a value
        ///     greater than zero if this is greater than object.
        /// </returns>
        public int CompareTo(InMemoryOffset other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other is null)
                return 1;

            return Offset.CompareTo(other.Offset);
        }

        /// <inheritdoc />
        public int CompareTo(IOffset other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other is null)
                return 1;

            return other is InMemoryOffset otherInMemoryOffset
                ? CompareTo(otherInMemoryOffset)
                : throw new ArgumentException($"Object must be of type {nameof(InMemoryOffset)}");
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (ReferenceEquals(obj, null))
                return false;

            if (!(obj is InMemoryOffset other))
                return false;

            return CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Key, Value, Offset);
    }
}
