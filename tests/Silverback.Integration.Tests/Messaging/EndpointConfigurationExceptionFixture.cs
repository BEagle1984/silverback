// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging;

public class EndpointConfigurationExceptionFixture
{
    private string? Topic { get; set; } = "invalid";

    [Theory]
    [InlineData("invalid", "invalid")]
    [InlineData("", "")]
    [InlineData(null, "null")]
    public void Constructor_ShouldBuildMessage_WhenPropertyValueAndNameArePassed(string? value, string valueString)
    {
        Topic = value;

        EndpointConfigurationException exception = new("Topic cannot be invalid.", Topic, nameof(Topic));

        exception.Message.Should()
            .Be(
                $"Topic cannot be invalid.{Environment.NewLine}" +
                $"Configuration property: EndpointConfigurationExceptionFixture.Topic{Environment.NewLine}" +
                $"Value: {valueString}");
    }

    [Fact]
    public void Constructor_ShouldSetAdditionalProperties_WhenPropertyValueAndNameArePassed()
    {
        Topic = "invalid";

        EndpointConfigurationException exception = new("message", Topic, nameof(Topic));

        exception.PropertyName.Should().Be("Topic");
        exception.PropertyValue.Should().Be("invalid");
        exception.ConfigurationType.Should().Be(GetType());
    }
}
