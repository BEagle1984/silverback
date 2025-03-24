// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class Base64ExtensionsFixture
{
    [Fact]
    public void ToBase64String_ShouldReturnBase64Representation()
    {
        byte[] array = [1, 2, 3, 4];

        string? result = array.ToBase64String();

        result.ShouldBe("AQIDBA==");
    }

    [Fact]
    public void ToBase64String_ShouldReturnNull_WhenArrayIsNull()
    {
        byte[]? array = null;

        string? result = array.ToBase64String();

        result.ShouldBeNull();
    }

    [Fact]
    public void FromBase64String_ShouldReturnByteArray()
    {
        string base64 = "AQIDBA==";

        byte[]? result = base64.FromBase64String();

        result.ShouldBe([1, 2, 3, 4]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FromBase64String_ShouldReturnNull_WhenStringIsNullOrEmpty(string? input)
    {
        byte[]? result = input.FromBase64String();

        result.ShouldBeNull();
    }
}
