// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ValueTaskFactoryFixture
{
    [Fact]
    public void CompletedTask_ShouldReturnDefaultValueTask()
    {
        ValueTask result = ValueTaskFactory.CompletedTask;

        result.Should().BeEquivalentTo(default(ValueTask));
    }

    [Fact]
    public async Task FromResult_ShouldReturnCompletedValueTask()
    {
        ValueTask<int> result = ValueTaskFactory.FromResult(42);

        result.IsCompleted.Should().BeTrue();
        result.IsCompletedSuccessfully.Should().BeTrue();
        (await result).Should().Be(42);
    }

    [Fact]
    public void FromCanceled_ShouldReturnCanceledValueTask()
    {
        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        ValueTask result = ValueTaskFactory.FromCanceled(cancellationTokenSource.Token);

        result.IsCompleted.Should().BeTrue();
        result.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void FromCanceled_ShouldReturnCanceledValueTaskWithResult()
    {
        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        ValueTask<int> result = ValueTaskFactory.FromCanceled<int>(cancellationTokenSource.Token);

        result.IsCompleted.Should().BeTrue();
        result.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void FromException_ShouldReturnFaultedValueTask()
    {
        ValueTask result = ValueTaskFactory.FromException(new ArgumentNullException());

        result.IsCompleted.Should().BeTrue();
        result.IsFaulted.Should().BeTrue();
    }

    [Fact]
    public void FromException_ShouldReturnFaultedValueTaskWithResult()
    {
        ValueTask<int> result = ValueTaskFactory.FromException<int>(new ArgumentNullException());

        result.IsCompleted.Should().BeTrue();
        result.IsFaulted.Should().BeTrue();
    }
}
