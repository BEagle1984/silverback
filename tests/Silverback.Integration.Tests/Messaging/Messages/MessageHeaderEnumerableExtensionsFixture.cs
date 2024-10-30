// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Extensions;
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

        result.Should().BeTrue();
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

        result.Should().BeFalse();
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

        result.Should().BeTrue();
        value.Should().Be("value2");
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

        result.Should().BeFalse();
        value.Should().BeNull();
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

        value.Should().Be("value2");
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

        value.Should().BeNull();
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

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetValue_ShouldConvertIntValue()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "42")
        ];

        int? value = headers.GetValue<int>("header1");

        value.Should().Be(42);
    }

    [Fact]
    public void GetValue_ShouldConvertDateTimeValue()
    {
        IEnumerable<MessageHeader> headers =
        [
            new("header1", "2023-06-23T02:42:42.1230000")
        ];

        DateTime? value = headers.GetValue<DateTime>("header1");

        value.Should().Be(23.June(2023).At(2, 42, 42, 123));
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

        value.Should().BeNull();
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

        value.Should().Be(2.2);
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

        value.Should().Be(0);
    }
}
