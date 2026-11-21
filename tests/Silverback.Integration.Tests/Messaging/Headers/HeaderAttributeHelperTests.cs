// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shouldly;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Headers;

public class HeaderAttributeHelperTests
{
    private enum RoundTripEnum
    {
        First,
        Second
    }

    [Fact]
    public void GetHeaders_ShouldReturnHeadersFromDecoratedMessage()
    {
        TestEventWithHeaders message = new()
        {
            StringHeader = "string1",
            StringHeaderWithDefault = "string2",
            IntHeader = 1,
            IntHeaderWithDefault = 2
        };

        IEnumerable<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message);

        result.ShouldBe(
            [
                new MessageHeader("x-string", "string1"),
                new MessageHeader("x-string-default", "string2"),
                new MessageHeader("x-readonly-string", "readonly"),
                new MessageHeader("x-int", "1"),
                new MessageHeader("x-int-default", "2"),
                new MessageHeader("x-readonly-int", "42")
            ],
            true);
    }

    [Fact]
    public void GetHeaders_ShouldDiscardHeadersWithDefaultValue()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = [.. HeaderAttributeHelper.GetHeaders(message)];

        result.Select(header => header.Name).ShouldNotContain("x-string");
        result.Select(header => header.Name).ShouldNotContain("x-int");
    }

    [Fact]
    public void GetHeaders_ShouldReturnHeadersWithDefaultValue_WhenPublishDefaultValueIsSet()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = [.. HeaderAttributeHelper.GetHeaders(message)];

        result.ShouldContain(new MessageHeader("x-string-default", null));
        result.ShouldContain(new MessageHeader("x-int-default", "0"));
    }

    [Fact]
    public void SetFromHeaders_ShouldMapFromDecoratedMessage()
    {
        MessageHeaderCollection headers = new()
        {
            { "x-string", "string1" },
            { "x-string-default", "string2" },
            { "x-readonly-string", "ignored" },
            { "x-int", "1" },
            { "x-int-default", "2" },
            { "x-readonly-int", "3" }
        };

        TestEventWithHeaders message = new();

        HeaderAttributeHelper.SetFromHeaders(message, headers);

        message.StringHeader.ShouldBe("string1");
        message.StringHeaderWithDefault.ShouldBe("string2");
        message.ReadOnlyStringHeader.ShouldBe("readonly");
        message.IntHeader.ShouldBe(1);
        message.IntHeaderWithDefault.ShouldBe(2);
        message.ReadOnlyIntHeader.ShouldBe(42);
    }

    [Fact]
    public void GetHeadersAndSetFromHeaders_ShouldRoundTripValues_WhenUsingNonInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            Guid guid = Guid.NewGuid();
            DateTime dateTime = new(2023, 6, 5, 2, 42, 42, 123);
            DateTimeOffset dateTimeOffset = new(2023, 6, 23, 2, 42, 42, 123, TimeSpan.FromHours(2));
            RoundTripMessage message = new()
            {
                NullableInt = 42,
                NullableDouble = 1234.5,
                NullableDecimal = 1234.5m,
                NullableBool = true,
                Guid = guid,
                NullableGuid = guid,
                TimeSpan = new TimeSpan(1, 2, 3, 4, 5),
                NullableTimeSpan = new TimeSpan(1, 2, 3, 4, 5),
                Enum = RoundTripEnum.Second,
                NullableEnum = RoundTripEnum.First,
                DateTime = dateTime,
                NullableDateTime = dateTime,
                DateTimeOffset = dateTimeOffset,
                NullableDateTimeOffset = dateTimeOffset
            };
            MessageHeaderCollection headers = [.. HeaderAttributeHelper.GetHeaders(message)];
            RoundTripMessage result = new()
            {
                NullableEnum = RoundTripEnum.Second
            };

            HeaderAttributeHelper.SetFromHeaders(result, headers);

            result.NullableInt.ShouldBe(42);
            result.NullableDouble.ShouldBe(1234.5);
            result.NullableDecimal.ShouldBe(1234.5m);
            result.NullableBool.ShouldBe(true);
            result.Guid.ShouldBe(guid);
            result.NullableGuid.ShouldBe(guid);
            result.TimeSpan.ShouldBe(new TimeSpan(1, 2, 3, 4, 5));
            result.NullableTimeSpan.ShouldBe(new TimeSpan(1, 2, 3, 4, 5));
            result.Enum.ShouldBe(RoundTripEnum.Second);
            result.NullableEnum.ShouldBe(RoundTripEnum.First);
            result.DateTime.ShouldBe(dateTime);
            result.NullableDateTime.ShouldBe(dateTime);
            result.DateTimeOffset.ShouldBe(dateTimeOffset);
            result.DateTimeOffset.Offset.ShouldBe(dateTimeOffset.Offset);
            result.NullableDateTimeOffset.ShouldBe(dateTimeOffset);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void GetHeadersAndSetFromHeaders_ShouldRoundTripDateTimeKindAndTicks(DateTimeKind kind)
    {
        DateTime dateTime = new(new DateTime(2023, 6, 23, 2, 42, 42, 123).Ticks + 4567, kind);
        RoundTripMessage message = new()
        {
            DateTime = dateTime,
            NullableDateTime = dateTime
        };
        MessageHeaderCollection headers = [.. HeaderAttributeHelper.GetHeaders(message)];
        RoundTripMessage result = new();

        HeaderAttributeHelper.SetFromHeaders(result, headers);

        result.DateTime.Kind.ShouldBe(kind);
        result.DateTime.Ticks.ShouldBe(dateTime.Ticks);
        result.NullableDateTime.ShouldNotBeNull();
        result.NullableDateTime.Value.Kind.ShouldBe(kind);
        result.NullableDateTime.Value.Ticks.ShouldBe(dateTime.Ticks);
    }

    [Fact]
    public void GetHeadersAndSetFromHeaders_ShouldRoundTripNullValues()
    {
        RoundTripMessage message = new();
        MessageHeaderCollection headers = [.. HeaderAttributeHelper.GetHeaders(message)];
        RoundTripMessage result = new()
        {
            NullableInt = 1,
            NullableDouble = 1,
            NullableDecimal = 1,
            NullableBool = true,
            NullableGuid = Guid.NewGuid(),
            NullableTimeSpan = TimeSpan.FromSeconds(1),
            NullableEnum = RoundTripEnum.Second,
            NullableDateTime = DateTime.UtcNow,
            NullableDateTimeOffset = DateTimeOffset.UtcNow
        };

        HeaderAttributeHelper.SetFromHeaders(result, headers);

        result.NullableInt.ShouldBeNull();
        result.NullableDouble.ShouldBeNull();
        result.NullableDecimal.ShouldBeNull();
        result.NullableBool.ShouldBeNull();
        result.NullableGuid.ShouldBeNull();
        result.NullableTimeSpan.ShouldBeNull();
        result.NullableEnum.ShouldBeNull();
        result.NullableDateTime.ShouldBeNull();
        result.NullableDateTimeOffset.ShouldBeNull();
    }

    private sealed class RoundTripMessage
    {
        [Header("x-int")]
        public int? NullableInt { get; set; }

        [Header("x-double")]
        public double? NullableDouble { get; set; }

        [Header("x-decimal")]
        public decimal? NullableDecimal { get; set; }

        [Header("x-bool")]
        public bool? NullableBool { get; set; }

        [Header("x-guid")]
        public Guid Guid { get; set; }

        [Header("x-guid-nullable")]
        public Guid? NullableGuid { get; set; }

        [Header("x-timespan")]
        public TimeSpan TimeSpan { get; set; }

        [Header("x-timespan-nullable")]
        public TimeSpan? NullableTimeSpan { get; set; }

        [Header("x-enum")]
        public RoundTripEnum Enum { get; set; }

        [Header("x-enum-nullable")]
        public RoundTripEnum? NullableEnum { get; set; }

        [Header("x-datetime")]
        public DateTime DateTime { get; set; }

        [Header("x-datetime-nullable")]
        public DateTime? NullableDateTime { get; set; }

        [Header("x-datetimeoffset")]
        public DateTimeOffset DateTimeOffset { get; set; }

        [Header("x-datetimeoffset-nullable")]
        public DateTimeOffset? NullableDateTimeOffset { get; set; }
    }
}
