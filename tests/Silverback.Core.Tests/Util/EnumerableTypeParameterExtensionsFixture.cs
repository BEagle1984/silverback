// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Test code")]
public class EnumerableTypeParameterExtensionsFixture
{
    private interface IObjectA
    {
    }

    private interface IObjectB
    {
    }

    [Fact]
    public void OfType_ShouldReturnObjectsOfMatchingType_WhenTypeSpecifiedViaParameter()
    {
        object[] objects = { new ObjectA(), new ObjectB(), new ObjectA(), new ObjectB() };

        IEnumerable<object> results = objects.OfType(typeof(ObjectA));

        results.Should().BeAssignableTo<IEnumerable<ObjectA>>();
        results.GetType().GenericTypeArguments[0].Should().Be(typeof(ObjectA));
        results.Should().HaveCount(2);
        results.Should().AllBeOfType<ObjectA>();
    }

    [Fact]
    public void OfType_ShouldReturnObjectsMatchingInterface_WhenInterfaceTypeSpecifiedViaParameter()
    {
        object[] objects = { new ObjectA(), new ObjectB(), new ObjectA(), new ObjectB() };

        IEnumerable<object> results = objects.OfType(typeof(IObjectA));

        results.Should().BeAssignableTo<IEnumerable<IObjectA>>();
        results.GetType().GenericTypeArguments[0].Should().Be(typeof(IObjectA));
        results.Should().HaveCount(2);
        results.Should().AllBeOfType<ObjectA>();
    }

    [Fact]
    public void Cast_ShouldReturnEnumerableOfMatchingType_WhenTypeSpecifiedViaParameter()
    {
        object[] objects = { new ObjectA(), new ObjectA() };

        IEnumerable<object> results = objects.Cast(typeof(IObjectA));

        results.Should().BeAssignableTo<IEnumerable<IObjectA>>();
        results.GetType().GenericTypeArguments[0].Should().Be(typeof(IObjectA));
    }

    [Fact]
    public void ToList_ShouldReturnEquivalentList_WhenTypeSpecifiedViaParameter()
    {
        ObjectA[] objects = { new(), new() };

        IEnumerable<object> results = objects.ToList(typeof(ObjectA));

        results.Should().BeOfType<List<ObjectA>>();
        results.Should().BeEquivalentTo(objects);
        results.Should().AllBeOfType<ObjectA>();
    }

    [Fact]
    public void ToList_ShouldReturnEquivalentListOfInterfaceType_WhenTypeSpecifiedViaParameter()
    {
        ObjectA[] objects = { new(), new() };

        IEnumerable<object> results = objects.ToList(typeof(IObjectA));

        results.Should().BeOfType<List<IObjectA>>();
        results.Should().BeEquivalentTo(objects);
        results.Should().AllBeOfType<ObjectA>();
    }

    private class ObjectA : IObjectA
    {
    }

    private class ObjectB : IObjectB
    {
    }
}
