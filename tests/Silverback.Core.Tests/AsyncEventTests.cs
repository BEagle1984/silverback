// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Core;

public class AsyncEventTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task InvokeAsync_ShouldInvokeAllHandlers()
    {
        AsyncEvent<string> asyncEvent = new();
        List<string> invocations = [];
        asyncEvent.AddHandler(arg =>
        {
            invocations.Add($"1:{arg}");
            return ValueTask.CompletedTask;
        });
        asyncEvent.AddHandler(arg =>
        {
            invocations.Add($"2:{arg}");
            return ValueTask.CompletedTask;
        });

        await asyncEvent.InvokeAsync("test");

        invocations.ShouldBe(["1:test", "2:test"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotInvokeRemovedHandler()
    {
        AsyncEvent<string> asyncEvent = new();
        int invocations = 0;
        asyncEvent.AddHandler(Handler);
        asyncEvent.RemoveHandler(Handler);

        await asyncEvent.InvokeAsync("test");

        invocations.ShouldBe(0);

        ValueTask Handler(string arg)
        {
            invocations++;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowHandlerToRemoveItself()
    {
        AsyncEvent<string> asyncEvent = new();
        int invocations = 0;
        int otherInvocations = 0;
        asyncEvent.AddHandler(SelfRemovingHandler);
        asyncEvent.AddHandler(_ =>
        {
            otherInvocations++;
            return ValueTask.CompletedTask;
        });

        await asyncEvent.InvokeAsync("test");
        await asyncEvent.InvokeAsync("test");

        invocations.ShouldBe(1);
        otherInvocations.ShouldBe(2);

        ValueTask SelfRemovingHandler(string arg)
        {
            invocations++;
            asyncEvent.RemoveHandler(SelfRemovingHandler);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The blocking invocation is awaited before the disposal")]
    public async Task InvokeAsync_ShouldNotBlockOtherCalls_WhenHandlerBlocks()
    {
        AsyncEvent<string> asyncEvent = new();
        using ManualResetEventSlim handlerEntered = new();
        using ManualResetEventSlim releaseHandler = new();
        bool released = false;
        asyncEvent.AddHandler(arg =>
        {
            if (arg == "blocking")
            {
                handlerEntered.Set();
                released = releaseHandler.Wait(Timeout);
            }

            return ValueTask.CompletedTask;
        });

        Task blockingInvocation = Task.Run(() => asyncEvent.InvokeAsync("blocking").AsTask());

        try
        {
            handlerEntered.Wait(Timeout).ShouldBeTrue();

            asyncEvent.AddHandler(_ => ValueTask.CompletedTask);
            await asyncEvent.InvokeAsync("other").AsTask().WaitAsync(Timeout);
        }
        finally
        {
            releaseHandler.Set();
        }

        await blockingInvocation.WaitAsync(Timeout);

        // The handler was released by the test, not by its timeout: the other calls didn't wait for it
        released.ShouldBeTrue();
    }
}
