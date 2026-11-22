// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequenceSettingsTests
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreDefault()
    {
        SequenceSettings settings = new();

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Validate_ShouldNotThrow_WhenTimeoutIsInRange(int milliseconds)
    {
        SequenceSettings settings = new() { Timeout = TimeSpan.FromMilliseconds(milliseconds) };

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldThrow_WhenTimeoutIsNotGreaterThanZero(int milliseconds)
    {
        SequenceSettings settings = new() { Timeout = TimeSpan.FromMilliseconds(milliseconds) };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("Sequence.Timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTimeoutExceedsInt32MaxValueMilliseconds()
    {
        SequenceSettings settings = new() { Timeout = TimeSpan.FromMilliseconds(int.MaxValue + 1.0) };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message
            .ShouldBe("Sequence.Timeout must be less than or equal to Int32.MaxValue milliseconds.");
    }
}
