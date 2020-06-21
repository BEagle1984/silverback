// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class TypesCacheTests
    {
        [Fact]
        public void GetType_ExistingType_TypeReturned()
        {
            var typeName = typeof(TestEventOne).AssemblyQualifiedName!;

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetType_WrongAssemblyVersion_TypeReturned()
        {
            var typeName =
                "Silverback.Tests.Core.TestTypes.Messages.TestEventOne, " +
                "Silverback.Core.Tests, Version=123.123.123.123";

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetType_NonExistingType_ExceptionThrown()
        {
            var typeName = "Baaaad.Event, Silverback.Core.Tests";

            Action act = () => TypesCache.GetType(typeName);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void GetType_IncompleteTypeName_TypeReturned()
        {
            var typeName = "Silverback.Tests.Core.TestTypes.Messages.TestEventOne, Silverback.Core.Tests";

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }
    }
}
