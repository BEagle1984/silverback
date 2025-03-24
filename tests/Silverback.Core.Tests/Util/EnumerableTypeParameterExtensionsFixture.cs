// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Test code")]
public class EnumerableTypeParameterExtensionsFixture
{
    private interface IObjectA;

    private interface IObjectB;

    [Fact]
    public void OfType_ShouldReturnObjectsOfMatchingType_WhenTypeSpecifiedViaParameter()
    {
        object[] objects = [new ObjectA(), new ObjectB(), new ObjectA(), new ObjectB()];

        IEnumerable<object> results = objects.OfType(typeof(ObjectA));

        results.ShouldBeAssignableTo<IEnumerable<ObjectA>>();
        results.GetType().GenericTypeArguments[0].ShouldBe(typeof(ObjectA));
        results.Count().ShouldBe(2);
        results.ShouldAllBe(result => result is ObjectA);
    }

    [Fact]
    public void OfType_ShouldReturnObjectsMatchingInterface_WhenInterfaceTypeSpecifiedViaParameter()
    {
        object[] objects = [new ObjectA(), new ObjectB(), new ObjectA(), new ObjectB()];

        IEnumerable<object> results = objects.OfType(typeof(IObjectA));

        results.ShouldBeAssignableTo<IEnumerable<IObjectA>>();
        results.GetType().GenericTypeArguments[0].ShouldBe(typeof(IObjectA));
        results.Count().ShouldBe(2);
        results.ShouldAllBe(result => result is ObjectA);
    }

    [Fact]
    public void Cast_ShouldReturnEnumerableOfMatchingType_WhenTypeSpecifiedViaParameter()
    {
        object[] objects = [new ObjectA(), new ObjectA()];

        IEnumerable<object> results = objects.Cast(typeof(IObjectA));

        results.ShouldBeAssignableTo<IEnumerable<IObjectA>>();
        results.GetType().GenericTypeArguments[0].ShouldBe(typeof(IObjectA));
    }

    [Fact]
    public void ToList_ShouldReturnEquivalentList_WhenTypeSpecifiedViaParameter()
    {
        ObjectA[] objects = [new(), new()];

        IEnumerable<object> results = objects.ToList(typeof(ObjectA));

        results.ShouldBeOfType<List<ObjectA>>();
        results.ShouldBe(objects);
        results.ShouldAllBe(result => result is ObjectA);
    }

    [Fact]
    public void ToList_ShouldReturnEquivalentListOfInterfaceType_WhenTypeSpecifiedViaParameter()
    {
        ObjectA[] objects = [new(), new()];

        IEnumerable<object> results = objects.ToList(typeof(IObjectA));

        results.ShouldBeOfType<List<IObjectA>>();
        results.ShouldBe(objects);
        results.ShouldAllBe(result => result is ObjectA);
    }

    private class ObjectA : IObjectA;

    private class ObjectB : IObjectB;
}
