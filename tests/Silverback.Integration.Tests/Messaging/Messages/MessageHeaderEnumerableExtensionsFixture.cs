// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderEnumerableExtensionsFixture
{
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

        Action act = () => headers.GetValue("header4", throwIfNotFound: true);

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
