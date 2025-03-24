// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class MessageHeaderFixture
{
    [Fact]
    public void Ctor_ShouldInitializeNameAndValue()
    {
        MessageHeader messageHeader = new("key", "value");

        messageHeader.Name.ShouldBe("key");
        messageHeader.Value.ShouldBe("value");
    }

    [Fact]
    public void Ctor_ShouldConvertDateTimeToStringWithInvariantCulture()
    {
        DateTime dateTime = new(1984, 6, 23, 2, 34, 56, 789);
        MessageHeader messageHeader = new("key", dateTime);

        messageHeader.Value.ShouldBe("1984-06-23T02:34:56.7890000");
    }

    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement", Justification = "Test code")]
    public void Ctor_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => new MessageHeader(null!, "value");

        act.ShouldThrow<ArgumentNullException>();
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

        result.ShouldBe(expected);
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

        result.ShouldBe(expected);
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

        result.ShouldBe(expected);
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

        result.ShouldBe(expected);
    }
}
