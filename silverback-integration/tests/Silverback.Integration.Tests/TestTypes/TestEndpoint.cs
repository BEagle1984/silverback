// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public sealed class TestEndpoint : IEndpoint, IEquatable<TestEndpoint>
    {
        public TestEndpoint(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

        public static TestEndpoint Default = new TestEndpoint("test");

        #region IEquatable

        public bool Equals(TestEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TestEndpoint && Equals((TestEndpoint)obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        #endregion
    }
}
