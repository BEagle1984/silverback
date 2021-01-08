// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging;

namespace Silverback.Tests.Types
{
    public sealed class TestConsumerEndpoint : ConsumerEndpoint, IEquatable<TestConsumerEndpoint>
    {
        public TestConsumerEndpoint(string name)
            : base(name)
        {
        }

        public string GroupId { get; set; } = "default-group";

        [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
        public static TestConsumerEndpoint GetDefault() => new("test");

        public override string GetUniqueConsumerGroupName() => $"{Name}|{GroupId}";

        public bool Equals(TestConsumerEndpoint? other)
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

            return Equals((TestConsumerEndpoint)obj);
        }

        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
