// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("ReSharper", "NotResolvedInText", Justification = "Test code")]
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Test code")]
public class CheckFixture
{
    [Fact]
    public void NotNull_ShouldReturnObject_WhenObjectIsNotNull()
    {
        object obj = new();

        object result = Check.NotNull(obj, "param");

        result.Should().BeSameAs(obj);
    }

    [Fact]
    public void NotNull_ShouldThrow_WhenObjectIsNull()
    {
        object? obj = null;

        Action act = () => Check.NotNull(obj, "param");

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NotEmpty_ShouldReturnCollection_WhenCollectionIsNotNullOrEmpty()
    {
        List<object> collection = [new(), new()];

        IReadOnlyCollection<object> result = Check.NotEmpty(collection, "param");

        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenCollectionIsEmpty()
    {
        List<object> collection = [];

        Action act = () => Check.NotEmpty(collection, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenCollectionIsNull()
    {
        List<object>? collection = null;

        Action act = () => Check.NotEmpty(collection, "param");

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NotEmpty_ShouldReturnString_WhenStringIsNotNullOrEmpty()
    {
        string str = "test";

        string result = Check.NotNullOrEmpty(str, "param");

        result.Should().BeSameAs(str);
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenStringIsEmpty()
    {
        string str = string.Empty;

        Action act = () => Check.NotNullOrEmpty(str, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenStringIsNull()
    {
        string? str = null;

        Action act = () => Check.NotNullOrEmpty(str, "param");

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void NotButNotEmpty_ShouldReturnString_WhenStringIsNotNullOrEmpty()
    {
        string str = "test";

        string? result = Check.NullButNotEmpty(str, "param");

        result.Should().BeSameAs(str);
    }

    [Fact]
    public void NotButNotEmpty_ShouldThrow_WhenStringIsEmpty()
    {
        string str = string.Empty;

        Action act = () => Check.NullButNotEmpty(str, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void NotButNotEmpty_ShouldReturnNull_WhenStringIsNull()
    {
        string? str = null;

        string? result = Check.NullButNotEmpty(str, "param");

        result.Should().BeNull();
    }

    [Fact]
    public void HasNoNulls_ShouldReturnCollection_WhenCollectionIsNotNullAndContainsNoNulls()
    {
        List<object> collection = [new(), new()];

        IReadOnlyCollection<object> result = Check.HasNoNulls(collection, "param");

        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void HasNoNulls_ShouldThrow_WhenCollectionContainsSomeNulls()
    {
        List<object?> collection = [new(), new(), null];

        Action act = () => Check.HasNoNulls(collection, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void HasNoNulls_ShouldThrow_WhenCollectionIsNull()
    {
        List<object>? collection = null;

        Action act = () => Check.HasNoNulls(collection, "param");

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldReturnCollection_WhenStringCollectionIsNotNullAndContainsNoNullOrEmptyStrings()
    {
        List<string> collection = ["abc", "def"];

        IReadOnlyCollection<string?> result = Check.HasNoEmpties(collection, "param");

        result.Should().BeSameAs(collection);
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionContainsSomeEmptyStrings()
    {
        List<string> collection = ["abc", "def", string.Empty];

        Action act = () => Check.HasNoEmpties(collection, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionContainsSomeNulls()
    {
        List<string?> collection = ["abc", "def", null];

        Action act = () => Check.HasNoEmpties(collection, "param");

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionIsNull()
    {
        List<string>? collection = null;

        Action act = () => Check.HasNoEmpties(collection, "param");

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(10, 0, 100)]
    [InlineData(-10, -100, 100)]
    [InlineData(0, 0, 100)]
    [InlineData(10, 10, 100)]
    [InlineData(100, 0, 100)]
    public void Range_ShouldReturnInt_WhenInputIsWithinRange(int value, int min, int max)
    {
        int result = Check.Range(value, "param", min, max);

        result.Should().Be(value);
    }

    [Theory]
    [InlineData(-1, 0, 100)]
    [InlineData(-101, -100, 100)]
    [InlineData(9, 10, 100)]
    [InlineData(101, 0, 100)]
    public void Range_ShouldThrow_WhenInputIsOutsideRange(int value, int min, int max)
    {
        Action act = () => Check.Range(value, "param", min, max);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }
}
