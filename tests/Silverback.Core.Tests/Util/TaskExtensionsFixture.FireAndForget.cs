// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "False positives")]
public partial class TaskExtensionsFixture
{
    [Fact]
    public void FireAndForget_ShouldTriggerTaskWithoutBlocking()
    {
        bool executed = false;
        SemaphoreSlim semaphore1 = new(0, 1);
        SemaphoreSlim semaphore2 = new(0, 1);

        async Task Execute()
        {
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(1));
            executed = true;
            semaphore2.Release();
        }

        Execute().FireAndForget();

        executed.Should().BeFalse();

        semaphore1.Release();
        semaphore2.Wait(TimeSpan.FromSeconds(1));

        executed.Should().BeTrue();
    }

    [Fact]
    public void FireAndForget_ShouldTriggerValueTaskWithoutBlocking()
    {
        bool executed = false;
        SemaphoreSlim semaphore1 = new(0, 1);
        SemaphoreSlim semaphore2 = new(0, 1);

        async ValueTask Execute()
        {
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(1));
            executed = true;
            semaphore2.Release();
        }

        Execute().FireAndForget();

        executed.Should().BeFalse();

        semaphore1.Release();
        semaphore2.Wait(TimeSpan.FromSeconds(1));

        executed.Should().BeTrue();
    }

    [Fact]
    public void FireAndForget_ShouldTriggerTaskWithReturnValueWithoutBlocking()
    {
        bool executed = false;
        SemaphoreSlim semaphore1 = new(0, 1);
        SemaphoreSlim semaphore2 = new(0, 1);

        async Task<int> Execute()
        {
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(1));
            executed = true;
            semaphore2.Release();
            return 42;
        }

        Execute().FireAndForget();

        executed.Should().BeFalse();

        semaphore1.Release();
        semaphore2.Wait(TimeSpan.FromSeconds(1));

        executed.Should().BeTrue();
    }

    [Fact]
    public void FireAndForget_ShouldTriggerValueTaskWithReturnValueWithoutBlocking()
    {
        bool executed = false;
        SemaphoreSlim semaphore1 = new(0, 1);
        SemaphoreSlim semaphore2 = new(0, 1);

        async ValueTask<int> Execute()
        {
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(1));
            executed = true;
            semaphore2.Release();
            return 42;
        }

        Execute().FireAndForget();

        executed.Should().BeFalse();

        semaphore1.Release();
        semaphore2.Wait(TimeSpan.FromSeconds(1));

        executed.Should().BeTrue();
    }
}
