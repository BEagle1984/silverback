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
