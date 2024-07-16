// Copyright (c) 2024 Sergio Aquilini
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

    [Theory]
    [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2")]
    [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests")]
    [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests")]
    [InlineData("Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2[[System.Int32, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], Silverback.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2, Silverback.Core.Tests")]
    [InlineData("Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2[[System.Int32, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", "Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2")]
    public void CleanAssemblyQualifiedName_ShouldReturnCorrectGenericType(string typeAssemblyQualifiedName, string expected)
    {
        string cleanedName = TypesCache.CleanAssemblyQualifiedName(typeAssemblyQualifiedName);

        cleanedName.Should().Be(expected);
    }

    private class TestObject;
}
