// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Test code")]
public class CheckTests
{
    [Fact]
    public void NotNull_ShouldReturnObject_WhenObjectIsNotNull()
    {
        object obj = new();

        object result = Check.NotNull(obj);

        result.ShouldBeSameAs(obj);
    }

    [Fact]
    public void NotNull_ShouldThrow_WhenObjectIsNull()
    {
        object? obj = null;

        Action act = () => Check.NotNull(obj);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void NotNull_ShouldSetParamNameFromArgumentExpression()
    {
        object? obj = null;

        Action act = () => Check.NotNull(obj);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("obj");
    }

    [Fact]
    public void NotNull_ShouldSetExplicitParamName()
    {
        object? obj = null;

        Action act = () => Check.NotNull(obj, "other");

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("other");
    }

    [Fact]
    public void NotEmpty_ShouldReturnCollection_WhenCollectionIsNotNullOrEmpty()
    {
        List<object> collection = [new(), new()];

        IReadOnlyCollection<object> result = Check.NotEmpty(collection);

        result.ShouldBeSameAs(collection);
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenCollectionIsEmpty()
    {
        List<object> collection = [];

        Action act = () => Check.NotEmpty(collection);

        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("collection");
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenCollectionIsNull()
    {
        List<object>? collection = null;

        Action act = () => Check.NotEmpty(collection);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("collection");
    }

    [Fact]
    public void NotEmpty_ShouldSetExplicitParamName_WhenCollectionIsNull()
    {
        List<object>? collection = null;

        Action act = () => Check.NotEmpty(collection, "other");

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("other");
    }

    [Fact]
    public void NotEmpty_ShouldReturnString_WhenStringIsNotNullOrEmpty()
    {
        string str = "test";

        string result = Check.NotNullOrEmpty(str);

        result.ShouldBeSameAs(str);
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenStringIsEmpty()
    {
        string str = "";

        Action act = () => Check.NotNullOrEmpty(str);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void NotEmpty_ShouldThrow_WhenStringIsNull()
    {
        string? str = null;

        Action act = () => Check.NotNullOrEmpty(str);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void NotButNotEmpty_ShouldReturnString_WhenStringIsNotNullOrEmpty()
    {
        string str = "test";

        string? result = Check.NullButNotEmpty(str);

        result.ShouldBeSameAs(str);
    }

    [Fact]
    public void NotButNotEmpty_ShouldThrow_WhenStringIsEmpty()
    {
        string str = "";

        Action act = () => Check.NullButNotEmpty(str);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void NotButNotEmpty_ShouldReturnNull_WhenStringIsNull()
    {
        string? str = null;

        string? result = Check.NullButNotEmpty(str);

        result.ShouldBeNull();
    }

    [Fact]
    public void HasNoNulls_ShouldReturnCollection_WhenCollectionIsNotNullAndContainsNoNulls()
    {
        List<object> collection = [new(), new()];

        IReadOnlyCollection<object> result = Check.HasNoNulls(collection);

        result.ShouldBeSameAs(collection);
    }

    [Fact]
    public void HasNoNulls_ShouldThrow_WhenCollectionContainsSomeNulls()
    {
        List<object?> collection = [new(), new(), null];

        Action act = () => Check.HasNoNulls(collection);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void HasNoNulls_ShouldThrow_WhenCollectionIsNull()
    {
        List<object>? collection = null;

        Action act = () => Check.HasNoNulls(collection);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldReturnCollection_WhenStringCollectionIsNotNullAndContainsNoNullOrEmptyStrings()
    {
        List<string> collection = ["abc", "def"];

        IReadOnlyCollection<string?> result = Check.HasNoEmpties(collection);

        result.ShouldBeSameAs(collection);
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionContainsSomeEmptyStrings()
    {
        List<string> collection = ["abc", "def", ""];

        Action act = () => Check.HasNoEmpties(collection);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionContainsSomeNulls()
    {
        List<string?> collection = ["abc", "def", null];

        Action act = () => Check.HasNoEmpties(collection);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void HasNoEmpties_ShouldThrow_WhenStringCollectionIsNull()
    {
        List<string>? collection = null;

        Action act = () => Check.HasNoEmpties(collection);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData(10, 0, 100)]
    [InlineData(-10, -100, 100)]
    [InlineData(0, 0, 100)]
    [InlineData(10, 10, 100)]
    [InlineData(100, 0, 100)]
    public void Range_ShouldReturnInt_WhenInputIsWithinRange(int value, int min, int max)
    {
        int result = Check.Range(value, min, max);

        result.ShouldBe(value);
    }

    [Theory]
    [InlineData(-1, 0, 100)]
    [InlineData(-101, -100, 100)]
    [InlineData(9, 10, 100)]
    [InlineData(101, 0, 100)]
    public void Range_ShouldThrow_WhenInputIsOutsideRange(int value, int min, int max)
    {
        Action act = () => Check.Range(value, min, max);

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("value");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-9, -10)]
    public void GreaterThan_ShouldReturnInt_WhenInputIsGreaterThanMin(int value, int min)
    {
        int result = Check.GreaterThan(value, min);

        result.ShouldBe(value);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-11, -10)]
    public void GreaterThan_ShouldThrow_WhenInputIsNotGreaterThanMin(int value, int min)
    {
        Action act = () => Check.GreaterThan(value, min);

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("value");
    }

    [Fact]
    public void GreaterThan_ShouldReturnNull_WhenNullableInputIsNull()
    {
        int? value = null;

        int? result = Check.GreaterThan(value, 0);

        result.ShouldBeNull();
    }

    [Fact]
    public void GreaterThan_ShouldReturnValue_WhenNullableInputIsGreaterThanMin()
    {
        int? value = 1;

        int? result = Check.GreaterThan(value, 0);

        result.ShouldBe(1);
    }

    [Fact]
    public void GreaterThan_ShouldThrow_WhenNullableInputIsNotGreaterThanMin()
    {
        int? value = 0;

        Action act = () => Check.GreaterThan(value, 0);

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("value");
    }

    [Fact]
    public void GreaterThan_ShouldSetExplicitParamName()
    {
        Action act = () => Check.GreaterThan(0, 0, "other");

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("other");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    public void GreaterOrEqualTo_ShouldReturnInt_WhenInputIsGreaterOrEqualToMin(int value, int min)
    {
        int result = Check.GreaterOrEqualTo(value, min);

        result.ShouldBe(value);
    }

    [Fact]
    public void GreaterOrEqualTo_ShouldThrow_WhenInputIsLessThanMin()
    {
        int value = -1;

        Action act = () => Check.GreaterOrEqualTo(value, 0);

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("value");
    }
}
