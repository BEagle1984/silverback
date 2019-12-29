// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;

#pragma warning disable 659

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestConsumerEndpoint : ConsumerEndpoint, IEquatable<TestConsumerEndpoint>
    {
        public TestConsumerEndpoint(string name) : base(name)
        {
        }

        public static TestConsumerEndpoint GetDefault() => new TestConsumerEndpoint("test");

        #region IEquatable

        public bool Equals(TestConsumerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TestConsumerEndpoint)obj);
        }

        #endregion
    }
}