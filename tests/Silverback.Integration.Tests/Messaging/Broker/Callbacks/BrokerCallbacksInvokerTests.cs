// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.Callbacks
{
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
            var callbackOneHandlerOne = new CallbackOneHandlerOne();
            var callbackOneHandlerTwo = new CallbackOneHandlerTwo();
            var callbackTwoHandlerOne = new CallbackTwoHandlerOne();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne)
                    .AddSingletonBrokerCallbackHandler(callbackOneHandlerTwo)
                    .AddSingletonBrokerCallbackHandler(callbackTwoHandlerOne));

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

            callbackOneHandlerOne.CallCount.Should().Be(1);
            callbackOneHandlerTwo.CallCount.Should().Be(1);
            callbackTwoHandlerOne.CallCount.Should().Be(0);
        }

        [Fact]
        public void Invoke_ScopedHandler_ScopeCreatedAndHandlerInvoked()
        {
            var callbackOneHandlerOne = new CallbackOneHandlerOne();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddScopedBrokerCallbackHandler(_ => callbackOneHandlerOne));

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

            callbackOneHandlerOne.CallCount.Should().Be(1);
        }

        [Fact]
        public void Invoke_WithServiceProvider_HandlerResolvedUsingSpecifiedProvider()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

            var callbackOneHandlerOne = new CallbackOneHandlerOne();
            var substituteServiceProvider = Substitute.For<IServiceProvider>();
            substituteServiceProvider
                .GetService(typeof(IEnumerable<IBrokerCallback>))
                .Returns(new[] { callbackOneHandlerOne });

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);

            callbackOneHandlerOne.CallCount.Should().Be(1);
        }

        [Fact]
        public void Invoke_NoMatchingHandler_TypesResolvedOnlyOnce()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

            var substituteServiceProvider = Substitute.For<IServiceProvider>();
            substituteServiceProvider
                .GetService(typeof(IEnumerable<IBrokerCallback>))
                .Returns(Array.Empty<IBrokerCallback>());

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);
            invoker.Invoke<ICallbackTwoHandler>(handler => handler.Handle(), substituteServiceProvider);
            invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle(), substituteServiceProvider);

            substituteServiceProvider.ReceivedWithAnyArgs(1).GetService(null!);
        }

        [Fact]
        public void Invoke_HandlerThrows_BrokerCallbackHandlerInvocationExceptionThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddTransientBrokerCallbackHandler<ThrowingCallbackOneHandler>());

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            Action act = () => invoker.Invoke<ICallbackOneHandler>(handler => handler.Handle());

            act.Should().ThrowExactly<BrokerCallbackInvocationException>();
        }

        [Fact]
        public async Task InvokeAsync_SomeHandlers_MatchingHandlersInvoked()
        {
            var callbackOneHandlerOne = new CallbackOneHandlerOneAsync();
            var callbackOneHandlerTwo = new CallbackOneHandlerTwoAsync();
            var callbackTwoHandlerOne = new CallbackTwoHandlerOneAsync();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddSingletonBrokerCallbackHandler(callbackOneHandlerOne)
                    .AddSingletonBrokerCallbackHandler(callbackOneHandlerTwo)
                    .AddSingletonBrokerCallbackHandler(callbackTwoHandlerOne));

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            await invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

            callbackOneHandlerOne.CallCount.Should().Be(1);
            callbackOneHandlerTwo.CallCount.Should().Be(1);
            callbackTwoHandlerOne.CallCount.Should().Be(0);
        }

        [Fact]
        public async Task InvokeAsync_ScopedHandler_ScopeCreatedAndHandlerInvoked()
        {
            var callbackOneHandlerOne = new CallbackOneHandlerOneAsync();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddScopedBrokerCallbackHandler(_ => callbackOneHandlerOne));

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            await invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

            callbackOneHandlerOne.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task InvokeAsync_WithServiceProvider_HandlerResolvedUsingSpecifiedProvider()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

            var callbackOneHandlerOne = new CallbackOneHandlerOneAsync();
            var substituteServiceProvider = Substitute.For<IServiceProvider>();
            substituteServiceProvider
                .GetService(typeof(IEnumerable<IBrokerCallback>))
                .Returns(new[] { callbackOneHandlerOne });

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            await invoker.InvokeAsync<ICallbackOneHandlerAsync>(
                handler => handler.HandleAsync(),
                substituteServiceProvider);

            callbackOneHandlerOne.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task InvokeAsync_NoMatchingHandler_TypesResolvedOnlyOnce()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>()));

            var substituteServiceProvider = Substitute.For<IServiceProvider>();
            substituteServiceProvider
                .GetService(typeof(IEnumerable<IBrokerCallback>))
                .Returns(Array.Empty<IBrokerCallback>());

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

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
        public void InvokeAsync_HandlerThrows_BrokerCallbackHandlerInvocationExceptionThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                    .AddTransientBrokerCallbackHandler<ThrowingCallbackOneHandlerAsync>());

            var invoker = serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>();

            Func<Task> act = () =>
                invoker.InvokeAsync<ICallbackOneHandlerAsync>(handler => handler.HandleAsync());

            act.Should().ThrowExactly<BrokerCallbackInvocationException>();
        }

        private class CallbackOneHandlerOne : ICallbackOneHandler
        {
            public int CallCount { get; private set; }

            public void Handle() => CallCount++;
        }

        private class CallbackOneHandlerTwo : ICallbackOneHandler
        {
            public int CallCount { get; private set; }

            public void Handle() => CallCount++;
        }

        private class CallbackTwoHandlerOne : ICallbackTwoHandler
        {
            public int CallCount { get; private set; }

            public void Handle() => CallCount++;
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class ThrowingCallbackOneHandler : ICallbackOneHandler
        {
            public void Handle() => throw new InvalidOperationException("test");
        }

        private class CallbackOneHandlerOneAsync : ICallbackOneHandlerAsync
        {
            public int CallCount { get; private set; }

            public Task HandleAsync()
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        private class CallbackOneHandlerTwoAsync : ICallbackOneHandlerAsync
        {
            public int CallCount { get; private set; }

            public Task HandleAsync()
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        private class CallbackTwoHandlerOneAsync : ICallbackTwoHandlerAsync
        {
            public int CallCount { get; private set; }

            public Task HandleAsync()
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class ThrowingCallbackOneHandlerAsync : ICallbackOneHandlerAsync
        {
            public Task HandleAsync() => throw new InvalidOperationException("test");
        }
    }
}
