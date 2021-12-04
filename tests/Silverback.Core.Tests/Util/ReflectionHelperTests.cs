// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ReflectionHelperTests
{
    [Fact]
    public void IsAsync_ReturnsTrue_IfAsyncWithTask()
    {
        MethodInfo? methodInfo = GetType().GetMethod(nameof(AsyncWithTask), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeTrue();
    }

    [Fact]
    public void IsAsync_ReturnsFalse_IfAsyncWithoutTask()
    {
        MethodInfo? methodInfo =
            GetType().GetMethod(nameof(AsyncWithoutTask), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeFalse();
    }

    [Fact]
    public void IsAsync_ReturnsFalse_IfNotAsyncWithVoid()
    {
        MethodInfo? methodInfo =
            GetType().GetMethod(nameof(NotAsyncWithVoid), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeFalse();
    }

    [Fact]
    public void ReturnsTask_ReturnsTrue_IfNotAsyncWithTask()
    {
        MethodInfo? methodInfo =
            GetType().GetMethod(nameof(NotAsyncWithTask), BindingFlags.Static | BindingFlags.NonPublic);

        methodInfo.Should().NotBeNull();
        methodInfo!.ReturnsTask().Should().BeTrue();
    }

    private static void NotAsyncWithVoid()
    {
    }

    private static Task NotAsyncWithTask() => Task.CompletedTask;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    private static async Task AsyncWithTask()
    {
    }

    [SuppressMessage("", "VSTHRD100", Justification = "Test case")]
    private static async void AsyncWithoutTask()
    {
    }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
