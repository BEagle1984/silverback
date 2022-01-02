// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Callbacks;

public class BrokerCallbacksInvokerTests
{
    private interface ICallbackOneHandler : IBrokerCallback
    {
        void Handle();
    }

    private interface ICallbackTwoHandler : IBrokerCallback
    {
        void Handle();
    }

    private interface ICallbackOneHandlerAsync : IBrokerCallback
    {
        Task HandleAsync();
    }

    private interface ICallbackTwoHandlerAsync : IBrokerCallback
    {
        Task HandleAsync();
    }

    [Fact]
    public void Invoke_SomeHandlers_MatchingHandlersInvoked()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        CallbackOneHandlerTwo callbackOneHandlerTwo = new();
        CallbackTwoHandlerOne callbackTwoHandlerOne = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne)
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerTwo)
                .AddSingletonBrokerCallbackHandler(callbackTwoHandlerOne));

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.Should().Be(1);
        callbackOneHandlerTwo.CallCount.Should().Be(1);
        callbackTwoHandlerOne.CallCount.Should().Be(0);
    }

    [Fact]
    public void Invoke_ScopedHandler_ScopeCreatedAndHandlerInvoked()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddScopedBrokerCallbackHandler(_ => callbackOneHandlerOne));

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public void Invoke_WithServiceProvider_HandlerResolvedUsingSpecifiedProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

        CallbackOneHandlerOne callbackOneHandlerOne = new();
        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerCallback>))
            .Returns(new[] { callbackOneHandlerOne });

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public void Invoke_NoMatchingHandler_TypesResolvedOnlyOnce()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerCallback>))
            .Returns(Array.Empty<IBrokerCallback>());

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);
        invoker.Invoke<ICallbackTwoHandler>(handler => handler.Handle(), substituteServiceProvider);
        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);

        substituteServiceProvider.ReceivedWithAnyArgs(1).GetService(null!);
    }

    [Fact]
    public void Invoke_HandlerThrows_BrokerCallbackHandlerInvocationExceptionThrown()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddTransientBrokerCallbackHandler<ThrowingCallbackOneHandler>());

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        Action act = () => invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

        act.Should().ThrowExactly<BrokerCallbackInvocationException>();
    }

    [Fact]
    public void Invoke_DuringApplicationShutdown_CallbackInvoked()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();
        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public void Invoke_DisablingCallbackDuringShutdown_CallbackNotInvoked()
    {
        CallbackOneHandlerOne callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();
        invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), invokeDuringShutdown: false);

        callbackOneHandlerOne.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_SomeHandlers_MatchingHandlersInvoked()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        CallbackOneHandlerTwoAsync callbackOneHandlerTwo = new();
        CallbackTwoHandlerOneAsync callbackTwoHandlerOne = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne)
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerTwo)
                .AddSingletonBrokerCallbackHandler(callbackTwoHandlerOne));

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.Should().Be(1);
        callbackOneHandlerTwo.CallCount.Should().Be(1);
        callbackTwoHandlerOne.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_ScopedHandler_ScopeCreatedAndHandlerInvoked()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddScopedBrokerCallbackHandler(_ => callbackOneHandlerOne));

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_WithServiceProvider_HandlerResolvedUsingSpecifiedProvider()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerCallback>))
            .Returns(new[] { callbackOneHandlerOne });

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_NoMatchingHandler_TypesResolvedOnlyOnce()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

        IServiceProvider? substituteServiceProvider = Substitute.For<IServiceProvider>();
        substituteServiceProvider
            .GetService(typeof(IEnumerable<IBrokerCallback>))
            .Returns(Array.Empty<IBrokerCallback>());

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);
        await invoker.InvokeAsync<ICallbackTwoHandlerAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);
        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(
            handler => handler.HandleAsync(),
            substituteServiceProvider);

        substituteServiceProvider.ReceivedWithAnyArgs(1).GetService(null!);
    }

    [Fact]
    public async Task InvokeAsync_HandlerThrows_BrokerCallbackHandlerInvocationExceptionThrown()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddTransientBrokerCallbackHandler<ThrowingCallbackOneHandlerAsync>());

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

        Func<Task> act = () =>
            invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

        await act.Should().ThrowExactlyAsync<BrokerCallbackInvocationException>();
    }

    [Fact]
    public async Task InvokeAsync_DuringApplicationShutdown_CallbackInvoked()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();
        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

        callbackOneHandlerOne.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_DisablingCallbackDuringShutdown_CallbackNotInvoked()
    {
        CallbackOneHandlerOneAsync callbackOneHandlerOne = new();
        FakeHostApplicationLifetime hostApplicationLifetime = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne),
            hostApplicationLifetime);

        hostApplicationLifetime.TriggerApplicationStopping();

        IBrokerCallbacksInvoker invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();
        await invoker.InvokeAsync<ICallbackOneHandlerAsync>(
            handler => handler.HandleAsync(),
            invokeDuringShutdown: false);

        callbackOneHandlerOne.CallCount.Should().Be(0);
    }

    private sealed class CallbackOneHandlerOne : ICallbackOneHandler
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    private sealed class CallbackOneHandlerTwo : ICallbackOneHandler
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    private sealed class CallbackTwoHandlerOne : ICallbackTwoHandler
    {
        public int CallCount { get; private set; }

        public void Handle() => CallCount++;
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class ThrowingCallbackOneHandler : ICallbackOneHandler
    {
        public void Handle() => throw new InvalidOperationException("test");
    }

    private sealed class CallbackOneHandlerOneAsync : ICallbackOneHandlerAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CallbackOneHandlerTwoAsync : ICallbackOneHandlerAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CallbackTwoHandlerOneAsync : ICallbackTwoHandlerAsync
    {
        public int CallCount { get; private set; }

        public Task HandleAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class ThrowingCallbackOneHandlerAsync : ICallbackOneHandlerAsync
    {
        public Task HandleAsync() => throw new InvalidOperationException("test");
    }
}
