// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class InstanceIdentifierFixture
{
    [Fact]
    public void Constructor_ShouldInitializeWithUniqueValue()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        id1.Value.Should().NotBeNullOrEmpty();
        id2.Value.Should().NotBeNullOrEmpty();
        id2.Value.Should().NotBeEquivalentTo(id1.Value);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = id1;

        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("", "CA1508", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        InstanceIdentifier? id1 = new();

        id1.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameValue()
    {
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentValue()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        id1.Equals(id2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrue_WhenComparingWithSameValue()
    {
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_ShouldReturnFalse_WhenComparingWithDifferentValue()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnFalse_WhenComparingWithSameValue()
    {
        Guid value = Guid.NewGuid();
        InstanceIdentifier id1 = new(value);
        InstanceIdentifier id2 = new(value);

        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrue_WhenComparingWithDifferentValue()
    {
        InstanceIdentifier id1 = new();
        InstanceIdentifier id2 = new();

        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        InstanceIdentifier id = new();

        id.ToString().Should().BeSameAs(id.Value);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnValue()
    {
        InstanceIdentifier id = new();
        string idString = id;

        idString.Should().BeSameAs(id.Value);
    }
}
