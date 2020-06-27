// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;

#pragma warning disable 659

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestOtherProducerEndpoint : ProducerEndpoint, IEquatable<TestOtherProducerEndpoint>
    {
        public TestOtherProducerEndpoint(string name)
            : base(name)
        {
        }

        public static TestOtherProducerEndpoint GetDefault() => new TestOtherProducerEndpoint("test");

        public bool Equals(TestOtherProducerEndpoint? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((TestOtherProducerEndpoint)obj);
        }
    }
}
