// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ReflectionHelperFixture
{
    [Fact]
    public void IsAsync_ShouldReturnTrueForAsyncMethodReturningTask()
    {
        MethodInfo? methodInfo = GetType().GetMethod(nameof(AsyncReturningTask), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeTrue();
    }

    [Fact]
    public void IsAsync_ShouldReturnFalseForAsyncMethodReturningVoid()
    {
        MethodInfo? methodInfo = GetType().GetMethod(nameof(AsyncReturningVoid), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeFalse();
    }

    [Fact]
    public void IsAsync_ShouldReturnFalseForSyncMethodReturningVoid()
    {
        MethodInfo? methodInfo = GetType().GetMethod(nameof(SyncReturningVoid), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeFalse();
    }

    [Fact]
    public void IsAsync_ShouldReturnFalseForSyncMethodReturningValue()
    {
        MethodInfo? methodInfo = GetType().GetMethod(nameof(SyncReturningInt), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeFalse();
    }

    private static void SyncReturningVoid()
    {
    }

    private static int SyncReturningInt() => 0;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    private static Task AsyncReturningTask() => Task.CompletedTask;

    [SuppressMessage("", "VSTHRD100", Justification = "Test case")]
    private static async void AsyncReturningVoid()
    {
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
