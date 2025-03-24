// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

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
            42.0,
            null);

        delay.ShouldBe(initialDelay);
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
            1.0,
            null);

        delay.TotalSeconds.ShouldBe(expectedDelay);
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
            delayFactor,
            null);

        delay.TotalSeconds.ShouldBe(expectedDelay);
    }

    [Fact]
    public void Compute_ShouldReturnMaxDelay_WhenMaxDelayExceeded()
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);
        TimeSpan delayIncrement = TimeSpan.FromSeconds(10);
        TimeSpan maxDelay = TimeSpan.FromSeconds(100);
        int failedAttempts = 10;

        TimeSpan delay = IncrementalDelayHelper.Compute(
            failedAttempts,
            initialDelay,
            delayIncrement,
            1.0,
            maxDelay);

        delay.ShouldBe(maxDelay);
    }

    [Fact]
    public void Compute_ShouldReturnComputedValue_WhenMaxDelayNotExceeded()
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);
        TimeSpan delayIncrement = TimeSpan.FromSeconds(10);
        TimeSpan maxDelay = TimeSpan.FromSeconds(200);
        int failedAttempts = 10;

        TimeSpan delay = IncrementalDelayHelper.Compute(
            failedAttempts,
            initialDelay,
            delayIncrement,
            1.0,
            maxDelay);

        delay.ShouldBe(TimeSpan.FromSeconds(142));
    }

    [Fact]
    public void Compute_ShouldReturnComputedValue_WhenMaxDelayIsNull()
    {
        TimeSpan initialDelay = TimeSpan.FromSeconds(42);
        TimeSpan delayIncrement = TimeSpan.FromSeconds(10);
        TimeSpan? maxDelay = null;
        int failedAttempts = 10;

        TimeSpan delay = IncrementalDelayHelper.Compute(
            failedAttempts,
            initialDelay,
            delayIncrement,
            1.0,
            maxDelay);

        delay.ShouldBe(TimeSpan.FromSeconds(142));
    }
}
