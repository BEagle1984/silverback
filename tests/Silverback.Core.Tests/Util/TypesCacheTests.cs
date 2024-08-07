﻿// Copyright (c) 2020 Sergio Aquilini
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

            type.AssemblyQualifiedName.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
        }

        [Fact]
        public void GetType_NonExistingType_ExceptionThrown()
        {
            var typeName = "Baaaad.Event, Silverback.Core.Tests";

            Action act = () => TypesCache.GetType(typeName);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void GetType_NonExistingTypeWithNoThrow_NullReturned()
        {
            var typeName = "Baaaad.Event, Silverback.Core.Tests";

            var type = TypesCache.GetType(typeName, false);

            type.Should().BeNull();
        }

        [Fact]
        public void GetType_IncompleteTypeName_TypeReturned()
        {
            var typeName = "Silverback.Tests.Core.TestTypes.Messages.TestEventOne, Silverback.Core.Tests";

            var type = TypesCache.GetType(typeName);

            type.Should().Be(typeof(TestEventOne));
        }

        [Theory]
        [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2")]
        [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests")]
        [InlineData("Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Silverback.Tests.Core.TestTypes.Messages.TestEventOne2, Silverback.Core.Tests")]
        [InlineData("Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2[[System.Int32, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], Silverback.Core.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2, Silverback.Core.Tests")]
        [InlineData("Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2[[System.Int32, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]", "Silverback.Tests.Core.Util.TypesCacheTests+MyMessage`2")]
        public void GetType_GenericType_TypeReturned(string typeAssemblyQualifiedName, string expected)
        {
            string cleanedName = TypesCache.CleanAssemblyQualifiedName(typeAssemblyQualifiedName);

            cleanedName.Should().Be(expected);
        }
    }
}
