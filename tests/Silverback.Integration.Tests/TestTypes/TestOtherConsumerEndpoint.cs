// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;

#pragma warning disable 659

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestOtherConsumerEndpoint : ConsumerEndpoint, IEquatable<TestOtherConsumerEndpoint>
    {
        public TestOtherConsumerEndpoint(string name)
            : base(name)
        {
        }

        public TestOtherConsumerEndpoint()
        {
        }

        public static TestOtherConsumerEndpoint GetDefault() => new TestOtherConsumerEndpoint("test");

        public override string GetUniqueConsumerGroupName() => Name;

        public bool Equals(TestOtherConsumerEndpoint? other)
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

            return Equals((TestOtherConsumerEndpoint)obj);
        }
    }
}
