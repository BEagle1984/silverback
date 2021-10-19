// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderFixture
{
    [Fact]
    public void Ctor_ShouldInitializeNameAndValue()
    {
        MessageHeader messageHeader = new("key", "value");
        messageHeader.Name.Should().Be("key");
        messageHeader.Value.Should().Be("value");
    }

    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement", Justification = "Test code")]
    public void Ctor_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => new MessageHeader(null!, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("one", "1", "one", "1", true)]
    [InlineData("one", null, "one", null, true)]
    [InlineData("one", "1", "two", "1", false)]
    [InlineData("one", "1", "one", "2", false)]
    [InlineData("one", null, "one", "2", false)]
    [InlineData("one", "1", "one", null, false)]
    public void Equals_ShouldCompareCorrectly(string xName, string? xValue, string yName, string? yValue, bool expected)
    {
        MessageHeader headerX = new(xName, xValue);
        MessageHeader headerY = new(yName, yValue);

        bool result = headerX.Equals(headerY);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("one", "1", "one", "1", true)]
    [InlineData("one", null, "one", null, true)]
    [InlineData("one", "1", "two", "1", false)]
    [InlineData("one", "1", "one", "2", false)]
    [InlineData("one", null, "one", "2", false)]
    [InlineData("one", "1", "one", null, false)]
    public void ObjectEquals_ShouldCompareCorrectly(string xName, string? xValue, string yName, string? yValue, bool expected)
    {
        MessageHeader headerX = new(xName, xValue);
        MessageHeader headerY = new(yName, yValue);

        bool result = headerX.Equals((object?)headerY);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("one", "1", "one", "1", true)]
    [InlineData("one", null, "one", null, true)]
    [InlineData("one", "1", "two", "1", false)]
    [InlineData("one", "1", "one", "2", false)]
    [InlineData("one", null, "one", "2", false)]
    [InlineData("one", "1", "one", null, false)]
    public void EqualityOperator_ShouldCompareCorrectly(string xName, string? xValue, string yName, string? yValue, bool expected)
    {
        MessageHeader headerX = new(xName, xValue);
        MessageHeader headerY = new(yName, yValue);

        bool result = headerX == headerY;

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("one", "1", "one", "1", false)]
    [InlineData("one", null, "one", null, false)]
    [InlineData("one", "1", "two", "1", true)]
    [InlineData("one", "1", "one", "2", true)]
    [InlineData("one", null, "one", "2", true)]
    [InlineData("one", "1", "one", null, true)]
    public void InequalityOperator_ShouldCompareCorrectly(string xName, string? xValue, string yName, string? yValue, bool expected)
    {
        MessageHeader headerX = new(xName, xValue);
        MessageHeader headerY = new(yName, yValue);

        bool result = headerX != headerY;

        result.Should().Be(expected);
    }
}
