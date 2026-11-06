// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook and the first call complete before the disposal")]
    public async Task ConnectAsync_ShouldConnectOnce_WhenCalledConcurrently()
    {
        HookLogger logger = new();
        await using TestBrokerClient client = new(logger);
        int initializedCount = 0;
        client.Initialized.AddHandler(_ =>
        {
            Interlocked.Increment(ref initializedCount);
            return ValueTask.CompletedTask;
        });

        using ManualResetEventSlim releaseFirstConnect = new();
        TaskCompletionSource firstConnectStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.OnLog(
            IntegrationLogEvents.BrokerClientInitializing,
            () =>
            {
                firstConnectStarted.TrySetResult();
                releaseFirstConnect.Wait(Timeout);
            });

        Task firstConnectTask = Task.Run(() => client.ConnectAsync().AsTask());

        try
        {
            await firstConnectStarted.Task.WaitAsync(Timeout);

            await client.ConnectAsync().AsTask().WaitAsync(Timeout);

            client.Status.ShouldBe(ClientStatus.Initializing);
        }
        finally
        {
            releaseFirstConnect.Set();
        }

        await firstConnectTask.WaitAsync(Timeout);

        client.ConnectCount.ShouldBe(1);
        initializedCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetInitializedStatusBeforeInvokingInitializedHandlers()
    {
        await using TestBrokerClient client = new(new HookLogger());
        ClientStatus? statusInHandler = null;
        client.Initialized.AddHandler(brokerClient =>
        {
            statusInHandler = brokerClient.Status;
            return ValueTask.CompletedTask;
        });

        await client.ConnectAsync();

        statusInHandler.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task ConnectAsync_ShouldResetStatus_WhenInitializingHandlerFails()
    {
        await using TestBrokerClient client = new(new HookLogger());
        client.Initializing.AddHandler(_ => throw new InvalidOperationException("Test"));

        await Should.ThrowAsync<InvalidOperationException>(() => client.ConnectAsync().AsTask());

        client.ConnectCount.ShouldBe(0);
        client.DisconnectCount.ShouldBe(0);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_ShouldDisconnect_WhenInitializedHandlerFails()
    {
        await using TestBrokerClient client = new(new HookLogger());
        int disconnectingCount = 0;
        client.Initialized.AddHandler(_ => throw new InvalidOperationException("Test"));
        client.Disconnecting.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectingCount);
            return ValueTask.CompletedTask;
        });

        await Should.ThrowAsync<InvalidOperationException>(() => client.ConnectAsync().AsTask());

        disconnectingCount.ShouldBe(1);
        client.DisconnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_ShouldBeAbleToConnectAgain_WhenInitializedHandlerFailed()
    {
        await using TestBrokerClient client = new(new HookLogger());
        int initializedCount = 0;
        client.Initialized.AddHandler(_ =>
        {
            if (Interlocked.Increment(ref initializedCount) == 1)
                throw new InvalidOperationException("Test");

            return ValueTask.CompletedTask;
        });

        await Should.ThrowAsync<InvalidOperationException>(() => client.ConnectAsync().AsTask());
        await client.ConnectAsync();

        client.ConnectCount.ShouldBe(2);
        client.DisconnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRethrowHandlerException_WhenDisconnectFailsAfterInitializedHandlerFailed()
    {
        await using TestBrokerClient client = new(new HookLogger());
        client.Initialized.AddHandler(_ => throw new InvalidOperationException("Handler failed."));
        client.Disconnecting.AddHandler(_ => throw new NotSupportedException("Disconnect failed."));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => client.ConnectAsync().AsTask());

        exception.Message.ShouldBe("Handler failed.");
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotResetStatus_WhenInitializedHandlerFailsWhileDisconnecting()
    {
        await using TestBrokerClient client = new(new HookLogger());
        TaskCompletionSource failInitializedHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseDisconnecting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Initialized.AddHandler(async _ =>
        {
            await failInitializedHandler.Task;

            throw new InvalidOperationException("Test");
        });

        client.Disconnecting.AddHandler(async _ => await releaseDisconnecting.Task);

        Task connectTask = client.ConnectAsync().AsTask();
        Task disconnectTask = client.DisconnectAsync().AsTask();

        client.Status.ShouldBe(ClientStatus.Disconnecting);

        failInitializedHandler.SetResult();
        await Should.ThrowAsync<AggregateException>(() => connectTask.WaitAsync(Timeout));

        client.Status.ShouldBe(ClientStatus.Disconnecting);

        releaseDisconnecting.SetResult();
        await disconnectTask.WaitAsync(Timeout);

        client.DisconnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConnectAsync_ShouldNotResetStatus_WhenInitializedHandlerFailsAfterReconnect()
    {
        await using TestBrokerClient client = new(new HookLogger());
        TaskCompletionSource failInitializedHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int initializedCount = 0;
        client.Initialized.AddHandler(async _ =>
        {
            // Only the first connect fails
            if (Interlocked.Increment(ref initializedCount) > 1)
                return;

            await failInitializedHandler.Task;

            throw new InvalidOperationException("Test");
        });

        Task connectTask = client.ConnectAsync().AsTask();

        await client.ReconnectAsync().AsTask().WaitAsync(Timeout);

        failInitializedHandler.SetResult();
        await Should.ThrowAsync<AggregateException>(() => connectTask.WaitAsync(Timeout));

        client.ConnectCount.ShouldBe(2);
        client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldDisconnect_WhenInitializedHandlerIsPending()
    {
        await using TestBrokerClient client = new(new HookLogger());
        TaskCompletionSource releaseInitializedHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Initialized.AddHandler(async _ => await releaseInitializedHandler.Task);

        Task connectTask = client.ConnectAsync().AsTask();

        await client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        client.DisconnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);

        releaseInitializedHandler.SetResult();
        await connectTask.WaitAsync(Timeout);

        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook and the first call complete before the disposal")]
    public async Task DisconnectAsync_ShouldDisconnectOnce_WhenCalledConcurrently()
    {
        HookLogger logger = new();
        await using TestBrokerClient client = new(logger);
        int disconnectingCount = 0;
        int disconnectedCount = 0;
        client.Disconnecting.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectingCount);
            return ValueTask.CompletedTask;
        });

        client.Disconnected.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectedCount);
            return ValueTask.CompletedTask;
        });

        await client.ConnectAsync();

        using ManualResetEventSlim releaseFirstDisconnect = new();
        TaskCompletionSource firstDisconnectStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.OnLog(
            IntegrationLogEvents.BrokerClientDisconnecting,
            () =>
            {
                firstDisconnectStarted.TrySetResult();
                releaseFirstDisconnect.Wait(Timeout);
            });

        Task firstDisconnectTask = Task.Run(() => client.DisconnectAsync().AsTask());

        try
        {
            await firstDisconnectStarted.Task.WaitAsync(Timeout);

            await client.DisconnectAsync().AsTask().WaitAsync(Timeout);

            client.Status.ShouldBe(ClientStatus.Disconnecting);
        }
        finally
        {
            releaseFirstDisconnect.Set();
        }

        await firstDisconnectTask.WaitAsync(Timeout);

        client.DisconnectCount.ShouldBe(1);
        disconnectingCount.ShouldBe(1);
        disconnectedCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    // Guards against an implementation that waits for the ongoing disconnect, which would deadlock on a reentrant call
    [Fact]
    public async Task DisconnectAsync_ShouldNotDeadlock_WhenCalledFromDisconnectingHandler()
    {
        await using TestBrokerClient client = new(new HookLogger());
        client.Disconnecting.AddHandler(brokerClient => brokerClient.DisconnectAsync());

        await client.ConnectAsync();

        await client.DisconnectAsync().AsTask().WaitAsync(Timeout);

        client.DisconnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook and the disconnect complete before the disposal")]
    public async Task ReconnectAsync_ShouldNotDisconnectAgainNorReconnect_WhenDisconnecting()
    {
        HookLogger logger = new();
        await using TestBrokerClient client = new(logger);
        await client.ConnectAsync();

        using ManualResetEventSlim releaseDisconnect = new();
        TaskCompletionSource disconnectStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.OnLog(
            IntegrationLogEvents.BrokerClientDisconnecting,
            () =>
            {
                disconnectStarted.TrySetResult();
                releaseDisconnect.Wait(Timeout);
            });

        Task disconnectTask = Task.Run(() => client.DisconnectAsync().AsTask());

        try
        {
            await disconnectStarted.Task.WaitAsync(Timeout);

            await client.ReconnectAsync().AsTask().WaitAsync(Timeout);
        }
        finally
        {
            releaseDisconnect.Set();
        }

        await disconnectTask.WaitAsync(Timeout);

        client.DisconnectCount.ShouldBe(1);
        client.ConnectCount.ShouldBe(1);
        client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    private sealed class TestBrokerClient : BrokerClient
    {
        private int _connectCount;

        private int _disconnectCount;

        public TestBrokerClient(ISilverbackLogger logger)
            : base("test-client", logger)
        {
        }

        public int ConnectCount => _connectCount;

        public int DisconnectCount => _disconnectCount;

        protected override ValueTask ConnectCoreAsync()
        {
            Interlocked.Increment(ref _connectCount);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisconnectCoreAsync()
        {
            Interlocked.Increment(ref _disconnectCount);
            return ValueTask.CompletedTask;
        }
    }

    // Runs the registered action (once) when the specified event is logged, to deterministically reproduce specific interleavings
    private sealed class HookLogger : ISilverbackLogger, ILogger
    {
        private readonly ConcurrentDictionary<int, Action> _hooks = new();

        public ILogger InnerLogger => this;

        public void OnLog(LogEvent logEvent, Action action) => _hooks[logEvent.EventId.Id] = action;

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (_hooks.TryRemove(eventId.Id, out Action? action))
                action.Invoke();
        }
    }
}
