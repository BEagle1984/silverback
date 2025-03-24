// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public partial class TypesCacheFixture
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetType_ShouldReturnNull_WhenTypeNameIsNullOrEmpty(string? typeName)
    {
        Type? type = TypesCache.GetType(typeName);

        type.ShouldBeNull();
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName()
    {
        string typeName = typeof(TestObject).AssemblyQualifiedName!;

        Type? type = TypesCache.GetType(typeName);

        type.ShouldBe(typeof(TestObject));
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName_WhenWrongAssemblyVersionIsSpecified()
    {
        string typeName = "Silverback.Tests.Core.Util.TypesCacheFixture+TestObject, Silverback.Core.Tests, Version=42.42.42.42";

        Type? type = TypesCache.GetType(typeName);

        type.ShouldNotBeNull();
        type.AssemblyQualifiedName.ShouldBe(typeof(TestObject).AssemblyQualifiedName);
    }

    [Fact]
    public void GetType_ShouldThrow_WhenTypeDoesNotExist()
    {
        string typeName = "Not.Existent.Type, Silverback.Core.Tests";

        Action act = () => TypesCache.GetType(typeName);

        act.ShouldThrow<TypeLoadException>();
    }

    [Fact]
    public void GetType_ShouldReturnNull_WhenTypeDoesNotExistButThrowOnErrorIsFalse()
    {
        string typeName = "Not.Existent.Type, Silverback.Core.Tests";

        Type? type = TypesCache.GetType(typeName, false);

        type.ShouldBeNull();
    }

    [Fact]
    public void GetType_ShouldReturnTypeMatchingAssemblyQualifiedName_WhenIncompleteAssemblyQualifiedNameIsSpecified()
    {
        string typeName = "Silverback.Tests.Core.Util.TypesCacheFixture+TestObject, Silverback.Core.Tests";

        Type? type = TypesCache.GetType(typeName);

        type.ShouldBe(typeof(TestObject));
    }

    [Theory]
    [MemberData(nameof(GetType_ShouldReturnCorrectGenericType_TestData))]
    [SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Unit test member data.")]
    public void GetType_ShouldReturnCorrectGenericType(string assemblyQualifiedName, Type expected)
    {
        Type? type = TypesCache.GetType(assemblyQualifiedName);

        type.ShouldNotBeNull();
        type.ShouldBe(expected);
    }

    private class TestObject;

    private class GenericTypeTest<T1, T2>
        where T2 : class
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Needed for testing")]
        public T1? P1 { get; set; }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Needed for testing")]
        public T2? P2 { get; set; }
    }
}
