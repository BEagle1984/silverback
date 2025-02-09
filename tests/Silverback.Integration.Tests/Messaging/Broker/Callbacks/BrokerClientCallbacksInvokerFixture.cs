// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Callbacks;

public class BrokerClientCallbacksInvokerFixture
{
    private interface ICallbackOne : IBrokerClientCallback
    {
        void Handle();
    }

    private interface ICallbackTwo : IBrokerClientCallback
    {
        void Handle();
    }

    private interface ICallbackOneAsync : IBrokerClientCallback
    {
        Task HandleAsync();
    }

    private interface ICallbackTwoAsync : IBrokerClientCallback
    {
        Task HandleAsync();
    }

    [Fact]
    public void Invoke_ShouldInvokeMatchingHandlers()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        CallbackOneHandlerTwo callbackOneHandlerTwo = new();
        CallbackTwoHandlerOne callbackTwoHandlerOne = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne)
                .AddSingletonBrokerClientCallback(callbackOneHandlerTwo)
                .AddSingletonBrokerClientCallback(callbackTwoHandlerOne));

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        invoker.Invoke<ICallbackOne>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
        callbackOneHandlerTwo.CallCount.ShouldBe(1);
        callbackTwoHandlerOne.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Invoke_ShouldCreateScope()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddScopedBrokerClientCallback(_ => callbackOneHandlerOne));

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        invoker.Invoke<ICallbackOne>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public void Invoke_ShouldResolveUsingSpecifiedProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        CallbackOneHandlerOne callbackOneHandlerOne = new();
        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerClientCallback>))
            .Returns(new[] { callbackOneHandlerOne });

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        invoker.Invoke<ICallbackOne>(handler => handler.Handle(), substituteServiceProvider);

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public void Invoke_ShouldResolveOnlyOnce_WhenNoMatchingHandler()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerClientCallback>))
            .Returns(Array.Empty<IBrokerClientCallback>());

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        invoker.Invoke<ICallbackOne>(handler => handler.Handle(), substituteServiceProvider);
        invoker.Invoke<ICallbackTwo>(handler => handler.Handle(), substituteServiceProvider);
        invoker.Invoke<ICallbackOne>(handler => handler.Handle(), substituteServiceProvider);

        substituteServiceProvider.ReceivedWithAnyArgs(1).GetService(null!);
    }

    [Fact(Skip = "TODO: Check why this was the expected behavior")]
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "To be reviewed")]
    public void Invoke_ShouldSwallowHandlerExceptions()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddTransientBrokerClientCallback<ThrowingCallbackOneHandler>());

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        Action act = () => invoker.Invoke<ICallbackOne>(handler => handler.Handle());

        act.ShouldNotThrow();
    }

    [Fact]
    public void Invoke_ShouldInvokeCallbackDuringShutdown()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();
        invoker.Invoke<ICallbackOne>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public void Invoke_ShouldNotInvokeCallbackDuringShutdown_WhenExplicitlyDisabled()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();
        invoker.Invoke<ICallbackOne>(handler => handler.Handle(), invokeDuringShutdown: false);

        callbackOneHandlerOne.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task InvokeAsync_ShouldInvokeMatchingHandlers()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        CallbackOneHandlerTwoAsync callbackOneHandlerTwo = new();
        CallbackTwoHandlerOneAsync callbackTwoHandlerOne = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne)
                .AddSingletonBrokerClientCallback(callbackOneHandlerTwo)
                .AddSingletonBrokerClientCallback(callbackTwoHandlerOne));

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
        callbackOneHandlerTwo.CallCount.ShouldBe(1);
        callbackTwoHandlerOne.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCreateScoped()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddScopedBrokerClientCallback(_ => callbackOneHandlerOne));

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolvedUsingSpecifiedProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerClientCallback>))
            .Returns(new[] { callbackOneHandlerOne });

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolveOnlyOnce_WhenNoMatchingHandler()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerClientCallback>))
            .Returns(Array.Empty<IBrokerClientCallback>());

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);
        await invoker.InvokeAsync<ICallbackTwoAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);
        await invoker.InvokeAsync<ICallbackOneAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);

        substituteServiceProvider.ReceivedWithAnyArgs(1).GetService(null!);
    }

    [Fact(Skip = "TODO: Check why this was the expected behavior")]
    [SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "To be reviewed")]
    public async Task InvokeAsync_ShouldSwallowHandlerExceptions()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddTransientBrokerClientCallback<ThrowingCallbackOneHandlerAsync>());

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();

        Func<Task> act = () => invoker.InvokeAsync<ICallbackOneAsync>(handler => handler.HandleAsync()).AsTask();

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_ShouldInvokeCallbackDuringShutdown()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();
        await invoker.InvokeAsync<ICallbackOneAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotInvokeCallbackDuringShutdown_WhenExplicitlyDisabled()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker()
                .AddSingletonBrokerClientCallback(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerClientCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerClientCallbacksInvoker>();
        await invoker.InvokeAsync<ICallbackOneAsync>(
            handler => handler.HandleAsync(),
            invokeDuringShutdown: false);

        callbackOneHandlerOne.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Invoke_ObjectDisposedThrownCreatingScope_ExceptionIgnored()
    {
        IServiceScopeFactory? serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactory.CreateScope().ThrowsForAnyArgs(_ => new ObjectDisposedException("test"));
        IBrokerClientCallbacksInvoker invoker = new BrokerClientCallbacksInvoker(
            serviceScopeFactory,
            new FakeHostApplicationLifetime(),
            new SilverbackLoggerSubstitute<BrokerClientCallbacksInvoker>());

        Action act = () => invoker.Invoke<ICallbackOne>(callback => callback.Handle());

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task InvokeAsync_ObjectDisposedThrownCreatingScope_ExceptionIgnored()
    {
        IServiceScopeFactory? serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactory.CreateScope().ThrowsForAnyArgs(_ => new ObjectDisposedException("test"));
        IBrokerClientCallbacksInvoker invoker = new BrokerClientCallbacksInvoker(
            serviceScopeFactory,
            new FakeHostApplicationLifetime(),
            new SilverbackLoggerSubstitute<BrokerClientCallbacksInvoker>());

        Func<Task> act = () => invoker.InvokeAsync<ICallbackOneAsync>(callback => callback.HandleAsync()).AsTask();

        await act.ShouldNotThrowAsync();
    }

    private sealed class CallbackOneHandlerOne : ICallbackOne
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    private sealed class CallbackOneHandlerTwo : ICallbackOne
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    private sealed class CallbackTwoHandlerOne : ICallbackTwo
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    private sealed class ThrowingCallbackOneHandler : ICallbackOne
    {
        public void Handle() => throw new InvalidOperationException("test");
    }

    private sealed class CallbackOneHandlerOneAsync : ICallbackOneAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CallbackOneHandlerTwoAsync : ICallbackOneAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CallbackTwoHandlerOneAsync : ICallbackTwoAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingCallbackOneHandlerAsync : ICallbackOneAsync
    {
        public Task HandleAsync() => throw new InvalidOperationException("test");
    }
}
