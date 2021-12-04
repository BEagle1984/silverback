// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace Silverback.Tests.Core;

public class InstanceIdentifierTests
{
    [Fact]
    public void Constructor_UniqueValueInitialized()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        id1.Value.Should().NotBeNullOrEmpty();
        id2.Value.Should().NotBeNullOrEmpty();
        id2.Value.Should().NotBeEquivalentTo(id1.Value);
    }

    [Fact]
    public void Equals_SameInstance_TrueReturned()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = id1;

        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("", "CA1508", Justification = "Test code")]
    public void Equals_Null_FalseReturned()
    {
        InstanceIdentifier? id1 = new();

        id1.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameValue_TrueReturned()
    {
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValue_FalseReturned()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

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
        Guid value = Guid.NewGuid();
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
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentValue_FalseReturned()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_SameValue_FalseReturned()
    {
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentValue_TrueReturned()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ValueReturned()
    {
        InstanceIdentifier id = new();

        id.ToString().Should().BeSameAs(id.Value);
    }

    [Fact]
    public void ImplicitConversionToString_ValueReturned()
    {
        InstanceIdentifier id = new();
        string idString = id;

        idString.Should().BeSameAs(id.Value);
    }
}
