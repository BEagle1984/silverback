// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Test methods")]
    public class EnumerableAsCollectionExtensionsTests
    {
        [Fact]
        public void AsReadOnlyCollection_Enumerable_NewInstanceReturned()
        {
            var enumerable = Enumerable.Range(1, 10);

            var collection = enumerable.AsReadOnlyCollection();

            collection.Should().NotBeSameAs(enumerable);
        }

        [Fact]
        public void AsReadOnlyCollection_Collection_SameInstanceReturned()
        {
            var enumerable = new List<int> { 1, 2, 3, 4 };

            var collection = enumerable.AsReadOnlyCollection();

            collection.Should().BeSameAs(enumerable);
        }

        [Fact]
        public void AsReadOnlyList_Enumerable_NewInstanceReturned()
        {
            var enumerable = Enumerable.Range(1, 10);

            var collection = enumerable.AsReadOnlyList();

            collection.Should().NotBeSameAs(enumerable);
        }

        [Fact]
        public void AsReadOnlyList_Collection_SameInstanceReturned()
        {
            var enumerable = new[] { 1, 2, 3, 4 };

            var collection = enumerable.AsReadOnlyList();

            collection.Should().BeSameAs(enumerable);
        }

        [Fact]
        public void AsList_Enumerable_NewInstanceReturned()
        {
            var enumerable = Enumerable.Range(1, 10);

            var collection = enumerable.AsList();

            collection.Should().NotBeSameAs(enumerable);
        }

        [Fact]
        public void AsList_List_SameInstanceReturned()
        {
            var enumerable = new List<int> { 1, 2, 3, 4 };

            var collection = enumerable.AsList();

            collection.Should().BeSameAs(enumerable);
        }
    }
}
