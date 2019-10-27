// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOffset : IOffset
    {
        public TestOffset(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public int CompareTo(IOffset other)
        {
            var thisValue = long.Parse(Value);
            var otherValue = long.Parse(other.Value);
            return thisValue.CompareTo(otherValue);
        }

        public string Key { get; }
        public string Value { get; }
    }
}