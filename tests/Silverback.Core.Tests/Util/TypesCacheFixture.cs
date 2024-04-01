// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class TypesCacheFixture
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetType_ShouldReturnNull_WhenTypeNameIsNullOrEmpty(string? typeName)
    {
        Type? type = TypesCache.GetType(typeName);

        type.Should().BeNull();
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName()
    {
        string typeName = typeof(TestObject).AssemblyQualifiedName!;

        Type? type = TypesCache.GetType(typeName);

        type.Should().Be(typeof(TestObject));
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName_WhenWrongAssemblyVersionIsSpecified()
    {
        string typeName = "Silverback.Tests.Core.Util.TypesCacheFixture+TestObject, Silverback.Core.Tests, Version=42.42.42.42";

        Type? type = TypesCache.GetType(typeName);

        type.ShouldNotBeNull();
        type.AssemblyQualifiedName.Should().Be(typeof(TestObject).AssemblyQualifiedName);
    }

    [Fact]
    public void GetType_ShouldThrow_WhenTypeDoesNotExist()
    {
        string typeName = "Not.Existent.Type, Silverback.Core.Tests";

        Action act = () => TypesCache.GetType(typeName);

        act.Should().Throw<TypeLoadException>();
    }

    [Fact]
    public void GetType_ShouldReturnNull_WhenTypeDoesNotExistButThrowOnErrorIsFalse()
    {
        string typeName = "Not.Existent.Type, Silverback.Core.Tests";

        Type? type = TypesCache.GetType(typeName, false);

        type.Should().BeNull();
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName_WhenIncompleteAssemblyQualifiedNameIsSpecified()
    {
        string typeName = "Silverback.Tests.Core.Util.TypesCacheFixture+TestObject, Silverback.Core.Tests";

        Type? type = TypesCache.GetType(typeName);

        type.Should().Be(typeof(TestObject));
    }

    private class TestObject;
}
