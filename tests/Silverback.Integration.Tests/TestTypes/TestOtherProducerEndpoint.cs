// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging;

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestOtherProducerEndpoint : ProducerEndpoint, IEquatable<TestOtherProducerEndpoint>
    {
        public TestOtherProducerEndpoint(string name)
            : base(name)
        {
        }

        [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
        public static TestOtherProducerEndpoint GetDefault() => new("test");

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

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
