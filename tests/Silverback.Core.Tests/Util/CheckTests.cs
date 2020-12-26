// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    [SuppressMessage("ReSharper", "NotResolvedInText", Justification = "Test code")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Test code")]
    public class CheckTests
    {
        [Fact]
        public void NotNull_Object_ObjectReturned()
        {
            object obj = new();

            var result = Check.NotNull(obj, "param");

            result.Should().BeSameAs(obj);
        }

        [Fact]
        public void NotNull_NullObject_ExceptionThrown()
        {
            object? obj = null;

            Action act = () => Check.NotNull(obj, "param");

            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void NotEmpty_Collection_CollectionReturned()
        {
            List<object> collection = new()
            {
                new object(), new object()
            };

            var result = Check.NotEmpty(collection, "param");

            result.Should().BeSameAs(collection);
        }

        [Fact]
        public void NotEmpty_EmptyCollection_ExceptionThrown()
        {
            List<object> collection = new();

            Action act = () => Check.NotEmpty(collection, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void NotEmpty_NullCollection_ExceptionThrown()
        {
            List<object>? collection = null;

            Action act = () => Check.NotEmpty(collection, "param");

            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void NotEmpty_String_StringReturned()
        {
            string str = "test";

            var result = Check.NotEmpty(str, "param");

            result.Should().BeSameAs(str);
        }

        [Fact]
        public void NotEmpty_EmptyString_ExceptionThrown()
        {
            string str = string.Empty;

            Action act = () => Check.NotEmpty(str, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void NotEmpty_NullString_ExceptionThrown()
        {
            string? str = null;

            Action act = () => Check.NotEmpty(str, "param");

            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void NullButNotEmpty_String_StringReturned()
        {
            string str = "test";

            var result = Check.NullButNotEmpty(str, "param");

            result.Should().BeSameAs(str);
        }

        [Fact]
        public void NullButNotEmpty_EmptyString_ExceptionThrown()
        {
            string str = string.Empty;

            Action act = () => Check.NullButNotEmpty(str, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void NullButNotEmpty_NullString_NullReturned()
        {
            string? str = null;

            var result = Check.NullButNotEmpty(str, "param");

            result.Should().BeNull();
        }

        [Fact]
        public void HasNoNulls_CollectionWithoutNulls_CollectionReturned()
        {
            List<object> collection = new() { new object(), new object() };

            var result = Check.HasNoNulls(collection, "param");

            result.Should().BeSameAs(collection);
        }

        [Fact]
        public void HasNoNulls_CollectionWithNulls_ExceptionThrown()
        {
            List<object?> collection = new() { new object(), new object(), null };

            Action act = () => Check.HasNoNulls(collection, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void HasNoNulls_NullCollection_ExceptionThrown()
        {
            List<object>? collection = null;

            Action act = () => Check.HasNoNulls(collection, "param");

            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void HasNoEmpties_StringCollectionWithoutEmpties_CollectionReturned()
        {
            List<string> collection = new() { "abc", "def" };

            var result = Check.HasNoEmpties(collection, "param");

            result.Should().BeSameAs(collection);
        }

        [Fact]
        public void HasNoEmpties_StringCollectionWithEmpties_ExceptionThrown()
        {
            List<string> collection = new() { "abc", "def", string.Empty };

            Action act = () => Check.HasNoEmpties(collection, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void HasNoEmpties_StringCollectionWithNulls_ExceptionThrown()
        {
            List<string?> collection = new() { "abc", "def", null };

            Action act = () => Check.HasNoEmpties(collection, "param");

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void HasNoEmpties_NullStringCollection_ExceptionThrown()
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
        public void Range_IntWithinBounds_IntReturned(int value, int min, int max)
        {
            var result = Check.Range(value, "param", min, max);

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(-1, 0, 100)]
        [InlineData(-101, -100, 100)]
        [InlineData(9, 10, 100)]
        [InlineData(101, 0, 100)]
        public void Range_IntOutsideBounds_ExceptionThrown(int value, int min, int max)
        {
            Action act = () => Check.Range(value, "param", min, max);

            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
        }
    }
}
