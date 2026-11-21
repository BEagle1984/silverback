// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderValueConversionExtensionsTests
{
    [Fact]
    public void ToHeaderValueString_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        DateTime value = new(2023, 6, 23, 2, 42, 42, 123);

        string? result = value.ToHeaderValueString();

        result.ShouldBe("2023-06-23T02:42:42.1230000");
    }

    [Fact]
    public void ToHeaderValueString_ShouldConvertDateTimeOffsetToStringWithInvariantCulture_WhenUsingNonInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            DateTimeOffset value = new(2023, 6, 23, 2, 42, 42, 123, TimeSpan.FromHours(2));

            string? result = value.ToHeaderValueString();

            result.ShouldBe("2023-06-23T02:42:42.1230000+02:00");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ToHeaderValueString_ShouldConvertFormattableToStringWithInvariantCulture_WhenUsingNonInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            1234.5.ToHeaderValueString().ShouldBe("1234.5");
            1234.5m.ToHeaderValueString().ShouldBe("1234.5");
            1234.5f.ToHeaderValueString().ShouldBe("1234.5");
            TimeSpan.FromSeconds(1.5).ToHeaderValueString().ShouldBe("00:00:01.5000000");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
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
