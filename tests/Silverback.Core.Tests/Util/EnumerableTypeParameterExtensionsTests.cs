// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class EnumerableTypeParameterExtensionsTests
    {
        [Fact]
        public void OfType_SpecificType_ObjectsOfMatchingTypeReturned()
        {
            var objects = new object[]
            {
                new TestEventOne(), new TestEventTwo(), new TestCommandOne(), new TestCommandTwo()
            };

            var result = objects.OfType(typeof(TestEventOne));

            result.Should().HaveCount(1);
        }

        [Fact]
        public void OfType_BaseType_ObjectsOfMatchingTypeReturned()
        {
            var objects = new object[]
            {
                new TestEventOne(), new TestEventTwo(), new TestCommandOne(), new TestCommandTwo()
            };

            var result = objects.OfType(typeof(IEvent));

            result.Should().HaveCount(2);
        }

        [Fact]
        public void Cast_Type_EnumerableOfMatchingTypeReturned()
        {
            var objects = new object[]
            {
                new TestEventOne(), new TestEventTwo()
            };

            var result = objects.OfType(typeof(IEvent));

            result.Should().BeAssignableTo<IEnumerable<IEvent>>();
        }

        [Fact]
        public void ToList_Type_ListOfMatchingTypeReturned()
        {
            var objects = new IEvent[]
            {
                new TestEventOne(), new TestEventTwo()
            };

            var result = objects.ToList(typeof(IEvent));

            result.Should().BeAssignableTo<List<IEvent>>();
        }
    }
}
