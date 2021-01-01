// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace Silverback.Tests.Core
{
    public class InstanceIdentifierTests
    {
        [Fact]
        public void Constructor_UniqueValueInitialized()
        {
            var id1 = new InstanceIdentifier();
            var id2 = new InstanceIdentifier();

            id1.Value.Should().NotBeNullOrEmpty();
            id2.Value.Should().NotBeNullOrEmpty();
            id2.Value.Should().NotBeEquivalentTo(id1.Value);
        }

        [Fact]
        public void Equals_SameInstance_TrueReturned()
        {
            var id1 = new InstanceIdentifier();
            var id2 = id1;

            id1.Equals(id2).Should().BeTrue();
        }

        [Fact]
        [SuppressMessage("", "CA1508", Justification = "Test code")]
        public void Equals_Null_FalseReturned()
        {
            var id1 = new InstanceIdentifier();

            id1.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameValue_TrueReturned()
        {
            var value = Guid.NewGuid();
            var id1 = new InstanceIdentifier(value);
            var id2 = new InstanceIdentifier(value);

            id1.Equals(id2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentValue_FalseReturned()
        {
            var id1 = new InstanceIdentifier();
            var id2 = new InstanceIdentifier();

            id1.Equals(id2).Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_SameInstance_TrueReturned()
        {
            object id1 = new InstanceIdentifier();
            object id2 = id1;

            id1.Equals(id2).Should().BeTrue();
        }

        [Fact]
        [SuppressMessage("", "CA1508", Justification = "Test code")]
        public void ObjectEquals_Null_FalseReturned()
        {
            object id1 = new InstanceIdentifier();

            id1.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_SameValue_TrueReturned()
        {
            var value = Guid.NewGuid();
            object id1 = new InstanceIdentifier(value);
            object id2 = new InstanceIdentifier(value);

            id1.Equals(id2).Should().BeTrue();
        }

        [Fact]
        public void ObjectEquals_DifferentValue_FalseReturned()
        {
            object id1 = new InstanceIdentifier();
            object id2 = new InstanceIdentifier();

            id1.Equals(id2).Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_SameValue_TrueReturned()
        {
            var value = Guid.NewGuid();
            var id1 = new InstanceIdentifier(value);
            var id2 = new InstanceIdentifier(value);

            (id1 == id2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_DifferentValue_FalseReturned()
        {
            var id1 = new InstanceIdentifier();
            var id2 = new InstanceIdentifier();

            (id1 == id2).Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_SameValue_FalseReturned()
        {
            var value = Guid.NewGuid();
            var id1 = new InstanceIdentifier(value);
            var id2 = new InstanceIdentifier(value);

            (id1 != id2).Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_DifferentValue_TrueReturned()
        {
            var id1 = new InstanceIdentifier();
            var id2 = new InstanceIdentifier();

            (id1 != id2).Should().BeTrue();
        }

        [Fact]
        public void ToString_ValueReturned()
        {
            var id = new InstanceIdentifier();

            id.ToString().Should().BeSameAs(id.Value);
        }

        [Fact]
        public void ImplicitConversionToString_ValueReturned()
        {
            var id = new InstanceIdentifier();
            var idString = (string)id;

            idString.Should().BeSameAs(id.Value);
        }
    }
}
