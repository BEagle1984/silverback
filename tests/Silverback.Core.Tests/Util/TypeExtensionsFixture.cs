// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class TypeExtensionsFixture
{
    [Theory]
    [InlineData("System.String", null)]
    [InlineData("System.Int32", 0)]
    [InlineData("System.Nullable`1[[System.Int32]]", null)]
    public void GetDefaultValue_ShouldReturnTypeDefaultValue(string typeName, object expected)
    {
        object? result = Type.GetType(typeName)!.GetDefaultValue();

        result.Should().Be(expected);
    }
}