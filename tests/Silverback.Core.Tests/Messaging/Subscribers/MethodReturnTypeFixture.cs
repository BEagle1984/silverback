// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Test code")]
[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test code")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
public class MethodReturnTypeFixture
{
    [Theory]
    [InlineData("ReturningObject", false, false, false)]
    [InlineData("ReturningInt", false, false, false)]
    [InlineData("ReturningTask", true, false, false)]
    [InlineData("ReturningTaskWithObjectResult", true, false, true)]
    [InlineData("ReturningTaskWithIntResult", true, false, true)]
    [InlineData("ReturningValueTask", false, true, false)]
    [InlineData("ReturningValueTaskWithObjectResult", false, true, true)]
    [InlineData("ReturningValueTaskWithIntResult", false, true, true)]
    public void CreateFromMethodInfo_ShouldReturnCorrectValues(string methodName, bool isTask, bool isValueTask, bool hasReturnValue)
    {
        MethodInfo methodInfo = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new ArgumentException("Method not found.");

        MethodReturnType returnType = MethodReturnType.CreateFromMethodInfo(methodInfo);

        returnType.IsTask.Should().Be(isTask);
        returnType.IsValueTask.Should().Be(isValueTask);
        returnType.HasResult.Should().Be(hasReturnValue);
    }

    private object ReturningObject() => new();

    private int ReturningInt() => 42;

    private Task ReturningTask() => Task.CompletedTask;

    private Task<object> ReturningTaskWithObjectResult() => Task.FromResult(new object());

    private Task<int> ReturningTaskWithIntResult() => Task.FromResult(42);

    private ValueTask ReturningValueTask() => ValueTask.CompletedTask;

    private ValueTask<object> ReturningValueTaskWithObjectResult() => ValueTask.FromResult(new object());

    private ValueTask<int> ReturningValueTaskWithIntResult() => ValueTask.FromResult(42);
}
