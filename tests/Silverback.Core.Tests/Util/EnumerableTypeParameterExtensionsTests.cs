// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class EnumerableTypeParameterExtensionsTests
{
    [Fact]
    public void OfType_SpecificType_ObjectsOfMatchingTypeReturned()
    {
        object[] objects =
        {
            new TestEventOne(), new TestEventTwo(), new TestCommandOne(), new TestCommandTwo()
        };

        IEnumerable<object> result = objects.OfType(typeof(TestEventOne));

        result.Should().HaveCount(1);
    }

    [Fact]
    public void OfType_BaseType_ObjectsOfMatchingTypeReturned()
    {
        object[] objects =
        {
            new TestEventOne(), new TestEventTwo(), new TestCommandOne(), new TestCommandTwo()
        };

        IEnumerable<object> result = objects.OfType(typeof(IEvent));

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Cast_Type_EnumerableOfMatchingTypeReturned()
    {
        object[] objects =
        {
            new TestEventOne(), new TestEventTwo()
        };

        IEnumerable<object> result = objects.OfType(typeof(IEvent));

        result.Should().BeAssignableTo<IEnumerable<IEvent>>();
    }

    [Fact]
    public void ToList_Type_ListOfMatchingTypeReturned()
    {
        IEvent[] objects =
        {
            new TestEventOne(), new TestEventTwo()
        };

        IEnumerable<object> result = objects.ToList(typeof(IEvent));

        result.Should().BeAssignableTo<List<IEvent>>();
    }
}
