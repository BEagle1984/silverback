// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class TaskExtensionsFixture
{
    [Theory]
    [InlineData("something")]
    [InlineData(null)]
    public async Task GetReturnValueAsync_ShouldUnwrapTaskReturnValue(string? value)
    {
        Task<string?> task = Task.FromResult(value);

        object? result = await task.GetReturnValueAsync();

        result.Should().Be(value);
    }

    [Fact]
    public async Task CancelOnException_ShouldCancelTasksAtFirstException()
    {
        bool success = false;

        using CancellationTokenSource cts = new();

        Task task1 = SuccessTask(cts.Token);
        Task task2 = FailingTask(cts.Token).CancelOnExceptionAsync(cts);

        Func<Task> act = () => Task.WhenAll(task1, task2);

        await act.Should().ThrowAsync<TestException>();
        success.Should().BeFalse();

        async Task SuccessTask(CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
            success = true;
        }

        static async Task FailingTask(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            throw new TestException();
        }
    }

    [Fact]
    public async Task CancelOnException_ShouldCancelTasksWithReturnValueAtFirstException()
    {
        bool success = false;

        using CancellationTokenSource cts = new();

        Task<int> task1 = SuccessTask(cts.Token);
        Task<int> task2 = FailingTask(cts.Token).CancelOnExceptionAsync(cts);

        Func<Task> act = () => Task.WhenAll(task1, task2);

        await act.Should().ThrowAsync<TestException>();
        success.Should().BeFalse();

        async Task<int> SuccessTask(CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
            success = true;
            return 1;
        }

        static async Task<int> FailingTask(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            throw new TestException();
        }
    }

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
