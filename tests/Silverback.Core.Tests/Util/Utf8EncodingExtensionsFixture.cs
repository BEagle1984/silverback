// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class Utf8EncodingExtensionsFixture
{
    [Fact]
    public void ToUtf8String_ShouldReturnString()
    {
        byte[] bytes = "ABC"u8.ToArray();

        string? result = bytes.ToUtf8String();

        result.ShouldBe("ABC");
    }

    [Fact]
    public void ToUtf8String_ShouldReturnNull_WhenBytesAreNull()
    {
        byte[]? bytes = null;

        string? result = bytes.ToUtf8String();

        result.ShouldBeNull();
    }

    [Fact]
    public void ToUtf8Bytes_ShouldReturnBytes()
    {
        byte[]? result = "ABC".ToUtf8Bytes();

        result.ShouldBe("ABC"u8.ToArray());
    }

    [Fact]
    public void ToUtf8Bytes_ShouldReturnNull_WhenStringIsNull()
    {
        string? str = null;

        byte[]? result = str.ToUtf8Bytes();

        result.ShouldBeNull();
    }
}
