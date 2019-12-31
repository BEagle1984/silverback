// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;

#pragma warning disable 659

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestProducerEndpoint : ProducerEndpoint, IEquatable<TestProducerEndpoint>
    {
        public TestProducerEndpoint(string name)
            : base(name)
        {
        }

        public static TestProducerEndpoint GetDefault() => new TestProducerEndpoint("test");

        #region IEquatable

        public bool Equals(TestProducerEndpoint other)
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
            return Equals((TestProducerEndpoint) obj);
        }

        #endregion
    }
}