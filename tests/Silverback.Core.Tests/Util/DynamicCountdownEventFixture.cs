// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class DynamicCountdownEventFixture
{
    [Fact]
    public void AddCount_ShouldIncreaseCurrentCount()
    {
        using DynamicCountdownEvent countdownEvent = new();

        countdownEvent.AddCount();
        countdownEvent.AddCount(3);

        countdownEvent.CurrentCount.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Reviewed")]
    public void AddCount_ShouldThrow_WhenCountIsZeroOrNegative(int count)
    {
        using DynamicCountdownEvent countdownEvent = new();

        Action act = () => countdownEvent.AddCount(count);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Signal_ShouldDecreaseCurrentCount()
    {
        using DynamicCountdownEvent countdownEvent = new();

        countdownEvent.AddCount(5);
        countdownEvent.Signal(2);
        countdownEvent.Signal();

        countdownEvent.CurrentCount.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Reviewed")]
    public void Signal_ShouldThrow_WhenCountIsZeroOrNegative(int count)
    {
        using DynamicCountdownEvent countdownEvent = new();

        Action act = () => countdownEvent.Signal(count);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Signal_ShouldReleaseSemaphore_WhenCountReachesZero()
    {
        DynamicCountdownEvent countdownEvent = new();

        countdownEvent.AddCount(2);

        Task waitTask = countdownEvent.WaitAsync();
        await AsyncTestingUtil.WaitAsync(() => waitTask.IsCompleted, TimeSpan.FromMilliseconds(50));
        waitTask.IsCompleted.Should().BeFalse();

        countdownEvent.Signal();
        await AsyncTestingUtil.WaitAsync(() => waitTask.IsCompleted, TimeSpan.FromMilliseconds(50));
        waitTask.IsCompleted.Should().BeFalse();

        countdownEvent.Signal();
        await AsyncTestingUtil.WaitAsync(() => waitTask.IsCompleted, TimeSpan.FromMilliseconds(200));
        waitTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task WaitAsync_ShouldCancelWait_WhenCancellationTokenIsSet()
    {
        DynamicCountdownEvent countdownEvent = new();
        countdownEvent.AddCount(2);

        Task waitTask = countdownEvent.WaitAsync(new CancellationTokenSource(10).Token);

        await AsyncTestingUtil.WaitAsync(() => waitTask.IsCompleted);
        waitTask.IsCompleted.Should().BeTrue();
        waitTask.Status.Should().Be(TaskStatus.Canceled);
    }
}
