// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class TestEventWithTypedHeaders : IIntegrationEvent
{
    public enum HeaderOption
    {
        First,
        Second
    }

    [JsonIgnore]
    [Header("x-int")]
    public int? NullableInt { get; set; }

    [JsonIgnore]
    [Header("x-double")]
    public double? NullableDouble { get; set; }

    [JsonIgnore]
    [Header("x-decimal")]
    public decimal? NullableDecimal { get; set; }

    [JsonIgnore]
    [Header("x-bool")]
    public bool? NullableBool { get; set; }

    [JsonIgnore]
    [Header("x-guid")]
    public Guid GuidValue { get; set; }

    [JsonIgnore]
    [Header("x-guid-nullable")]
    public Guid? NullableGuid { get; set; }

    [JsonIgnore]
    [Header("x-timespan")]
    public TimeSpan TimeSpan { get; set; }

    [JsonIgnore]
    [Header("x-timespan-nullable")]
    public TimeSpan? NullableTimeSpan { get; set; }

    [JsonIgnore]
    [Header("x-enum")]
    public HeaderOption Enum { get; set; }

    [JsonIgnore]
    [Header("x-enum-nullable")]
    public HeaderOption? NullableEnum { get; set; }

    [JsonIgnore]
    [Header("x-datetime")]
    public DateTime DateTime { get; set; }

    [JsonIgnore]
    [Header("x-datetimeoffset")]
    public DateTimeOffset DateTimeOffset { get; set; }

    [JsonIgnore]
    [Header("x-datetimeoffset-nullable")]
    public DateTimeOffset? NullableDateTimeOffset { get; set; }

    public string? Content { get; set; }
}
