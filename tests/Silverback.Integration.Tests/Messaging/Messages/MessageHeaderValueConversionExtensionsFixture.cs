// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderValueConversionExtensionsFixture
{
    [Fact]
    public void ToHeaderValueString_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        DateTime value = new(2023, 6, 23, 2, 42, 42, 123);

        string? result = value.ToHeaderValueString();

        result.ShouldBe("2023-06-23T02:42:42.1230000");
    }

    [Theory]
    [InlineData(12, "12")]
    [InlineData(12.34, "12.34")]
    [InlineData("test", "test")]
    [InlineData(null, null)]
    public void ToHeaderValueString_ShouldConvertToString(object? value, string? expected)
    {
        string? result = value.ToHeaderValueString();

        result.ShouldBe(expected);
    }
}
