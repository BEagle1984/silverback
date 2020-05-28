// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class TypeExtensionsTests
    {
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [Theory]
        [InlineData("System.String", null)]
        [InlineData("System.Int32", 0)]
        [InlineData("System.Nullable`1[[System.Int32]]", null)]
        public void GetDefaultValue_DefaultForTypeIsReturned(string typeName, object expected)
        {
            Type.GetType(typeName)!.GetDefaultValue().Should().Be(expected);
        }
    }
}
