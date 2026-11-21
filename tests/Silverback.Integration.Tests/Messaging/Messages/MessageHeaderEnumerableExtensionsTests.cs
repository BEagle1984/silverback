// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderEnumerableExtensionsTests
{
    private enum HeaderTestEnum
    {
        First,
        Second
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenHeaderExists()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        bool result = headers.Contains("header2");

        result.ShouldBeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenHeaderDoesNotExist()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        bool result = headers.Contains("header4");

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetValue_ShouldReturnTrueAndValue_WhenHeaderExists()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        bool result = headers.TryGetValue("header2", out string? value);

        result.ShouldBeTrue();
        value.ShouldBe("value2");
    }

    [Fact]
    public void TryGetValue_ShouldReturnFalseAndNull_WhenHeaderDoesNotExist()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        bool result = headers.TryGetValue("header4", out string? value);

        result.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void GetValue_ShouldReturnValue_WhenHeaderExists()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        string? value = headers.GetValue("header2");

        value.ShouldBe("value2");
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenHeaderDoesNotExist()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        string? value = headers.GetValue("header4");

        value.ShouldBeNull();
    }

    [Fact]
    public void GetValue_ShouldThrow_WhenHeaderDoesNotExistAndThrowIfNotFoundIsTrue()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        Action act = () => headers.GetValue("header4", true);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetValue_ShouldConvertIntValue()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "42")
        ];

        int? value = headers.GetValue<int>("header1");

        value.ShouldBe(42);
    }

    [Fact]
    public void GetValue_ShouldConvertDateTimeValue()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "2023-06-23T02:42:42.1230000")
        ];

        DateTime? value = headers.GetValue<DateTime>("header1");

        value.ShouldBe(new DateTime(2023, 6, 23, 2, 42, 42, 123));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void GetValue_ShouldRoundTripDateTimeKindAndTicks(DateTimeKind kind)
    {
        DateTime dateTime = new(new DateTime(2023, 6, 23, 2, 42, 42, 123).Ticks + 4567, kind);
        MessageHeaderCollection headers = new()
        {
            { "datetime", dateTime }
        };

        DateTime? value = headers.GetValue<DateTime>("datetime");
        object? nullableValue = headers.GetValue("datetime", typeof(DateTime?));

        value.ShouldNotBeNull();
        value.Value.Kind.ShouldBe(kind);
        value.Value.Ticks.ShouldBe(dateTime.Ticks);
        nullableValue.ShouldBeOfType<DateTime>().Kind.ShouldBe(kind);
        ((DateTime)nullableValue).Ticks.ShouldBe(dateTime.Ticks);
    }

    [Fact]
    public void GetValue_ShouldRoundTripValues_WhenUsingNonInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            Guid guid = Guid.NewGuid();
            DateTimeOffset dateTimeOffset = new(2023, 6, 23, 2, 42, 42, 123, TimeSpan.FromHours(2));
            MessageHeaderCollection headers = new()
            {
                { "int", (int?)42 },
                { "double", (double?)1234.5 },
                { "decimal", (decimal?)1234.5m },
                { "bool", (bool?)true },
                { "guid", guid },
                { "timespan", new TimeSpan(1, 2, 3, 4, 5) },
                { "enum", HeaderTestEnum.Second },
                { "enum-first", HeaderTestEnum.First },
                { "datetimeoffset", dateTimeOffset },
                { "null-int", (int?)null },
                { "null-double", (double?)null },
                { "null-decimal", (decimal?)null },
                { "null-bool", (bool?)null },
                { "null-guid", (Guid?)null },
                { "null-timespan", (TimeSpan?)null },
                { "null-enum", (HeaderTestEnum?)null },
                { "null-datetimeoffset", (DateTimeOffset?)null }
            };

            headers.GetValue("int", typeof(int?)).ShouldBe(42);
            headers.GetValue("double", typeof(double?)).ShouldBe(1234.5);
            headers.GetValue("decimal", typeof(decimal?)).ShouldBe(1234.5m);
            headers.GetValue("bool", typeof(bool?)).ShouldBe(true);
            headers.GetValue<Guid>("guid").ShouldBe(guid);
            headers.GetValue("guid", typeof(Guid?)).ShouldBe(guid);
            headers.GetValue<TimeSpan>("timespan").ShouldBe(new TimeSpan(1, 2, 3, 4, 5));
            headers.GetValue("timespan", typeof(TimeSpan?)).ShouldBe(new TimeSpan(1, 2, 3, 4, 5));
            headers.GetValue<HeaderTestEnum>("enum").ShouldBe(HeaderTestEnum.Second);
            headers.GetValue("enum", typeof(HeaderTestEnum?)).ShouldBe(HeaderTestEnum.Second);
            headers.GetValue<DateTimeOffset>("datetimeoffset").ShouldBe(dateTimeOffset);
            headers.GetValue<DateTimeOffset>("datetimeoffset")!.Value.Offset.ShouldBe(dateTimeOffset.Offset);
            headers.GetValue("datetimeoffset", typeof(DateTimeOffset?)).ShouldBe(dateTimeOffset);
            headers.GetValue("null-int", typeof(int?)).ShouldBeNull();
            headers.GetValue("null-double", typeof(double?)).ShouldBeNull();
            headers.GetValue("null-decimal", typeof(decimal?)).ShouldBeNull();
            headers.GetValue("null-bool", typeof(bool?)).ShouldBeNull();
            headers.GetValue("null-guid", typeof(Guid?)).ShouldBeNull();
            headers.GetValue("null-timespan", typeof(TimeSpan?)).ShouldBeNull();
            headers.GetValue("null-enum", typeof(HeaderTestEnum?)).ShouldBeNull();
            headers.GetValue("null-datetimeoffset", typeof(DateTimeOffset?)).ShouldBeNull();
            headers.GetValue("missing", typeof(Guid?)).ShouldBeNull();
            headers.GetValue("enum-first", typeof(HeaderTestEnum?)).ShouldBe(HeaderTestEnum.First);
            headers.GetValueOrDefault<Guid>("guid").ShouldBe(guid);
            headers.GetValueOrDefault<HeaderTestEnum>("enum").ShouldBe(HeaderTestEnum.Second);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenValueCannotBeConverted()
    {
        MessageHeaderCollection headers = new()
        {
            { "header1", "not-a-value" }
        };

        headers.GetValue("header1", typeof(int?)).ShouldBeNull();
        headers.GetValue<Guid>("header1").ShouldBeNull();
        headers.GetValue<TimeSpan>("header1").ShouldBeNull();
        headers.GetValue<HeaderTestEnum>("header1").ShouldBeNull();
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenHeaderDoesNotExistDespiteConversion()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "value1"),
            new("header2", "value2"),
            new("header3", "value3")
        ];

        double? value = headers.GetValue<double>("header4");

        value.ShouldBeNull();
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnValue_WhenHeaderExists()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "1.1"),
            new("header2", "2.2"),
            new("header3", "3.3")
        ];

        double? value = headers.GetValueOrDefault<double>("header2");

        value.ShouldBe(2.2);
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnDefaultValue_WhenHeaderDoesNotExist()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "1.1"),
            new("header2", "2.2"),
            new("header3", "3.3")
        ];

        double? value = headers.GetValueOrDefault<double>("header4");

        value.ShouldBe(0);
    }
}
