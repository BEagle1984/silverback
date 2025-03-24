// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "False positives")]
public partial class TaskExtensionsFixture
{
    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Tasks awaited in AwaitAllAsync")]
    public async Task AwaitAllAsync_ShouldAwaitAllValueTasks()
    {
        SemaphoreSlim semaphore = new(0, 3);

        ValueTask WaitSemaphore() => new(semaphore.WaitAsync());

        IEnumerable<ValueTask> tasks = [WaitSemaphore(), WaitSemaphore(), WaitSemaphore()];
        ValueTask awaitTask = tasks.AwaitAllAsync();

        awaitTask.IsCompleted.ShouldBeFalse();

        semaphore.Release();
        semaphore.Release();

        await Task.Delay(50);

        awaitTask.IsCompleted.ShouldBeFalse();

        semaphore.Release();

        await awaitTask;

        awaitTask.IsCompleted.ShouldBeTrue();
        awaitTask.IsCompletedSuccessfully.ShouldBeTrue();
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

        IEnumerable<ValueTask> tasks = [Dummy(), Throw(), Dummy()];

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(1);
        tasks.Count(task => task.IsCompleted).ShouldBe(3);
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

        IEnumerable<ValueTask> tasks = [Dummy(), Throw(), Throw()];

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(2);
        tasks.Count(task => task.IsCompleted).ShouldBe(3);
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

        IEnumerable<ValueTask<int>> tasks = [WaitSemaphore(1), WaitSemaphore(2), WaitSemaphore(3)];
        ValueTask<IReadOnlyCollection<int>> awaitTask = tasks.AwaitAllAsync();

        awaitTask.IsCompleted.ShouldBeFalse();

        semaphore.Release();
        semaphore.Release();

        await Task.Delay(50);

        awaitTask.IsCompleted.ShouldBeFalse();

        semaphore.Release();

        IReadOnlyCollection<int> results = await awaitTask;

        awaitTask.IsCompleted.ShouldBeTrue();
        awaitTask.IsCompletedSuccessfully.ShouldBeTrue();
        results.ShouldBe([1, 2, 3], ignoreOrder: true);
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

        IEnumerable<ValueTask<int>> tasks = [Dummy(), Throw(), Dummy()];

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(1);
        tasks.Count(task => task.IsCompleted).ShouldBe(3);
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

        IEnumerable<ValueTask<int>> tasks = [Dummy(), Throw(), Throw()];

        Func<Task> act = () => tasks.AwaitAllAsync().AsTask();

        AggregateException aggregateException = await act.ShouldThrowAsync<AggregateException>();
        aggregateException.InnerExceptions.Count.ShouldBe(2);
        tasks.Count(task => task.IsCompleted).ShouldBe(3);
    }
}
