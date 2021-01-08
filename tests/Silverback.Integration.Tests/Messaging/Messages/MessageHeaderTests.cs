// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class MessageHeaderTests
    {
        [Fact]
        public void Ctor_FillsProperties_IfParametersAreValid()
        {
            var messageHeader = new MessageHeader("key", "value");
            messageHeader.Name.Should().Be("key");
            messageHeader.Value.Should().Be("value");
        }

        [Fact]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement", Justification = "Test code")]
        public void Ctor_Throws_IfKeyIsNull()
        {
            Action act = () => new MessageHeader(null!, "value");

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Key_Throws_IfValueIsNull()
        {
            var messageHeader = new MessageHeader("key", "value");

            Action act = () => messageHeader.Name = null!;

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void KeyValue_FillsProperties_IfParametersAreValid()
        {
            var messageHeader = new MessageHeader("key", "value");
            messageHeader.Name = "key1";
            messageHeader.Value = "value1";
            messageHeader.Name.Should().Be("key1");
            messageHeader.Value.Should().Be("value1");
        }

        [Theory]
        [InlineData("one", "1", "one", "1", true)]
        [InlineData("one", null, "one", null, true)]
        [InlineData("one", "1", "two", "1", false)]
        [InlineData("one", "1", "one", "2", false)]
        [InlineData("one", "1", null, null, false)]
        public void Equals_ValuesCorrectlyCompared(
            string xName,
            string? xValue,
            string? yName,
            string? yValue,
            bool expected)
        {
            var headerX = new MessageHeader(xName, xValue);
            var headerY = yName != null ? new MessageHeader(yName, yValue) : null;

            var result = headerX.Equals(headerY);

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("one", "1", "one", "1", true)]
        [InlineData("one", null, "one", null, true)]
        [InlineData("one", "1", "two", "1", false)]
        [InlineData("one", "1", "one", "2", false)]
        [InlineData("one", "1", null, null, false)]
        public void ObjectEquals_ValuesCorrectlyCompared(
            string xName,
            string? xValue,
            string? yName,
            string? yValue,
            bool expected)
        {
            var headerX = new MessageHeader(xName, xValue);
            var headerY = yName != null ? new MessageHeader(yName, yValue) : null;

            var result = headerX.Equals((object?)headerY);

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("one", "1", "one", "1", true)]
        [InlineData("one", null, "one", null, true)]
        [InlineData("one", "1", "two", "1", false)]
        [InlineData("one", "1", "one", "2", false)]
        [InlineData(null, null, "one", "1", false)]
        [InlineData("one", "1", null, null, false)]
        public void EqualityOperator_ValuesCorrectlyCompared(
            string? xName,
            string? xValue,
            string? yName,
            string? yValue,
            bool expected)
        {
            var headerX = xName != null ? new MessageHeader(xName, xValue) : null;
            var headerY = yName != null ? new MessageHeader(yName, yValue) : null;

            var result = headerX == headerY;

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("one", "1", "one", "1", false)]
        [InlineData("one", null, "one", null, false)]
        [InlineData("one", "1", "two", "1", true)]
        [InlineData("one", "1", "one", "2", true)]
        [InlineData(null, null, "one", "1", true)]
        [InlineData("one", "1", null, null, true)]
        public void InequalityOperator_ValuesCorrectlyCompared(
            string? xName,
            string? xValue,
            string? yName,
            string? yValue,
            bool expected)
        {
            var headerX = xName != null ? new MessageHeader(xName, xValue) : null;
            var headerY = yName != null ? new MessageHeader(yName, yValue) : null;

            var result = headerX != headerY;

            result.Should().Be(expected);
        }
    }
}
