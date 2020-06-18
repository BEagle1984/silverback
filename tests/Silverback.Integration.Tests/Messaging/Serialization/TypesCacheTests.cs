// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization
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
                "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, " +
                "Silverback.Integration.Tests, Version=123.123.123.123";

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetType_NonExistingType_ExceptionThrown()
        {
            var typeName = "Baaaad.Event, Silverback.Integration.Tests";

            Action act = () => TypesCache.GetType(typeName);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void GetType_IncompleteTypeName_TypeReturned()
        {
            var typeName = "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests";

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }
    }
}
