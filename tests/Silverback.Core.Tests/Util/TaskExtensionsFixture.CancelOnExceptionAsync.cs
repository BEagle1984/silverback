﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "False positives")]
public partial class TaskExtensionsFixture
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
}