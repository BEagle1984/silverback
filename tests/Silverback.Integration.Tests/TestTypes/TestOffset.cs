// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOffset : IComparableOffset
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

        public static bool operator <(TestOffset left, TestOffset right) =>
            Comparer<TestOffset>.Default.Compare(left, right) < 0;

        public static bool operator >(TestOffset left, TestOffset right) =>
            Comparer<TestOffset>.Default.Compare(left, right) > 0;

        public static bool operator <=(TestOffset left, TestOffset right) =>
            Comparer<TestOffset>.Default.Compare(left, right) <= 0;

        public static bool operator >=(TestOffset left, TestOffset right) =>
            Comparer<TestOffset>.Default.Compare(left, right) >= 0;

        public static bool operator ==(TestOffset left, TestOffset right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(TestOffset left, TestOffset right) => !(left == right);

        public int CompareTo(IOffset? other)
        {
            if (other == null)
                return 0;

            long thisValue = long.Parse(Value, CultureInfo.InvariantCulture);
            long otherValue = long.Parse(other.Value, CultureInfo.InvariantCulture);

            return thisValue.CompareTo(otherValue);
        }

        public string ToLogString() => Value;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (ReferenceEquals(obj, null))
                return false;

            if (!(obj is TestOffset other))
                return false;

            return CompareTo(other) == 0;
        }

        public override int GetHashCode() => HashCode.Combine(Key, Value);
    }
}
