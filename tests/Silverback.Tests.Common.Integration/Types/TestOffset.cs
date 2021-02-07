// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Types
{
    public class TestOffset : IBrokerMessageOffset
    {
        public TestOffset()
        {
            Key = "test";
            Value = Guid.NewGuid().ToString();
        }

        public TestOffset(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }

        public static bool operator <(TestOffset? left, TestOffset? right) =>
            Comparer<TestOffset>.Default.Compare(left, right) < 0;

        public static bool operator >(TestOffset? left, TestOffset? right) =>
            Comparer<TestOffset>.Default.Compare(left, right) > 0;

        public static bool operator <=(TestOffset? left, TestOffset? right) =>
            Comparer<TestOffset>.Default.Compare(left, right) <= 0;

        public static bool operator >=(TestOffset? left, TestOffset? right) =>
            Comparer<TestOffset>.Default.Compare(left, right) >= 0;

        public static bool operator ==(TestOffset? left, TestOffset? right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(TestOffset left, TestOffset right) => !(left == right);

        public int CompareTo(IBrokerMessageOffset? other)
        {
            if (other == null)
                return 0;

            long thisValue = long.Parse(Value, CultureInfo.InvariantCulture);
            long otherValue = long.Parse(other.Value, CultureInfo.InvariantCulture);

            return thisValue.CompareTo(otherValue);
        }

        public string ToLogString() => Value;

        public string ToVerboseLogString() => $"{Key}@{Value}";

        [SuppressMessage("", "CA1508", Justification = "False positive: is TestOffset")]
        public bool Equals(IBrokerMessageIdentifier? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (other is not TestOffset otherRabbitOffset)
                return false;

            return Key == otherRabbitOffset.Key && Value == otherRabbitOffset.Value;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((IBrokerMessageIdentifier)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Key, Value);
    }
}
