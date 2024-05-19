// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Consuming.ErrorHandling;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class IncrementalDelayHelperFixture
{
    [Fact]
    public void Compute_ShouldReturnInitialDelay_WhenFailedAttemptsIsZero()
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);

        TimeSpan delay = IncrementalDelayHelper.Compute(
            0,
            initialDelay,
            TimeSpan.FromSeconds(42),
            42.0);

        delay.Should().Be(initialDelay);
    }

    [Theory]
    [InlineData(0, 42)]
    [InlineData(1, 84)]
    [InlineData(2, 126)]
    public void Compute_ShouldIncrementDelay(int failedAttempts, int expectedDelay)
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);
        TimeSpan delayIncrement = TimeSpan.FromSeconds(42);

        TimeSpan delay = IncrementalDelayHelper.Compute(
            failedAttempts,
            initialDelay,
            delayIncrement,
            1.0);

        delay.TotalSeconds.Should().Be(expectedDelay);
    }

    [Theory]
    [InlineData(0, 42)]
    [InlineData(1, 105)]
    [InlineData(2, 262.5)]
    public void Compute_ShouldApplyDelayFactor(int failedAttempts, double expectedDelay)
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);
        double delayFactor = 2.5;

        TimeSpan delay = IncrementalDelayHelper.Compute(
            failedAttempts,
            initialDelay,
            TimeSpan.Zero,
            delayFactor);

        delay.TotalSeconds.Should().Be(expectedDelay);
    }
}
