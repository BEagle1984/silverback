// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public sealed class TestEndpoint : Endpoint, IEquatable<TestEndpoint>
    {
        public TestEndpoint(string name) : base(name)
        {
        }

        public static TestEndpoint Default = new TestEndpoint("test");

        public static TestEndpoint DefaultWithBatch = new TestEndpoint("test")
        {
            Batch = new Silverback.Messaging.Batch.BatchSettings
            {
                Size = 5
            }
        };

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
