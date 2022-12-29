// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "False positives")]
public class TaskExtensionsFixture
{
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

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldAwaitAllValueTasks()
    {
        SemaphoreSlim semaphore = new(0, 3);

        ValueTask WaitSemaphore() => new(semaphore.WaitAsync());

        IEnumerable<ValueTask> tasks = new[] { WaitSemaphore(), WaitSemaphore(), WaitSemaphore() };
        ValueTask awaitTask = tasks.AwaitAllAsync();

        awaitTask.IsCompleted.Should().BeFalse();

        semaphore.Release();
        semaphore.Release();

        await Task.Delay(50);

        awaitTask.IsCompleted.Should().BeFalse();

        semaphore.Release();

        await awaitTask;

        awaitTask.IsCompleted.Should().BeTrue();
        awaitTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldThrowAggregateException_WhenValueTaskThrows()
    {
        static async ValueTask Dummy() => await Task.Delay(10);

        static async ValueTask Throw()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test");
        }

        IEnumerable<ValueTask> tasks = new[] { Dummy(), Throw(), Dummy() };

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        (await act.Should().ThrowExactlyAsync<AggregateException>()).And.InnerExceptions.Should().HaveCount(1);
        tasks.Where(task => task.IsCompleted).Should().HaveCount(3);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldThrowAggregateException_WhenMultipleValueTasksThrow()
    {
        static async ValueTask Dummy() => await Task.Delay(10);

        static async ValueTask Throw()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test");
        }

        IEnumerable<ValueTask> tasks = new[] { Dummy(), Throw(), Throw() };

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        (await act.Should().ThrowExactlyAsync<AggregateException>()).And.InnerExceptions.Should().HaveCount(2);
        tasks.Where(task => task.IsCompleted).Should().HaveCount(3);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldAwaitAllValueTasksAndReturnResults()
    {
        SemaphoreSlim semaphore = new(0, 3);

        async ValueTask<int> WaitSemaphore(int value)
        {
            await semaphore.WaitAsync();
            return value;
        }

        IEnumerable<ValueTask<int>> tasks = new[] { WaitSemaphore(1), WaitSemaphore(2), WaitSemaphore(3) };
        ValueTask<IReadOnlyCollection<int>> awaitTask = tasks.AwaitAllAsync();

        awaitTask.IsCompleted.Should().BeFalse();

        semaphore.Release();
        semaphore.Release();

        await Task.Delay(50);

        awaitTask.IsCompleted.Should().BeFalse();

        semaphore.Release();

        IReadOnlyCollection<int> results = await awaitTask;

        awaitTask.IsCompleted.Should().BeTrue();
        awaitTask.IsCompletedSuccessfully.Should().BeTrue();
        results.Should().BeEquivalentTo(new[] { 1, 2, 3 }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldThrowAggregateException_WhenValueTaskWithResultThrows()
    {
        static async ValueTask<int> Dummy()
        {
            await Task.Delay(10);
            return 42;
        }

        static async ValueTask<int> Throw()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test");
        }

        IEnumerable<ValueTask<int>> tasks = new[] { Dummy(), Throw(), Dummy() };

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        (await act.Should().ThrowExactlyAsync<AggregateException>()).And.InnerExceptions.Should().HaveCount(1);
        tasks.Where(task => task.IsCompleted).Should().HaveCount(3);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldThrowAggregateException_WhenMultipleValueTasksWithResultThrow()
    {
        static async ValueTask<int> Dummy()
        {
            await Task.Delay(10);
            return 42;
        }

        static async ValueTask<int> Throw()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test");
        }

        IEnumerable<ValueTask<int>> tasks = new[] { Dummy(), Throw(), Throw() };

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        (await act.Should().ThrowExactlyAsync<AggregateException>()).And.InnerExceptions.Should().HaveCount(2);
        tasks.Where(task => task.IsCompleted).Should().HaveCount(3);
    }
}
