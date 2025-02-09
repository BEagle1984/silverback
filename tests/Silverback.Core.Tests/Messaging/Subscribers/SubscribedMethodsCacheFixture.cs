// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers;

public class SubscribedMethodsCacheFixture
{
    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnTrueForEnvelope_WhenEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEnumerable<TestEventOne>>(
                    _ =>
                    {
                    }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnTrueForEnvelope_WhenAsyncEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(_ => Task.CompletedTask));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnTrueForEnvelope_WhenMessageStreamSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(_ => Task.CompletedTask));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenNoStreamSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(_ => Task.CompletedTask)
                .AddDelegateSubscriber<TestEventTwo>(
                    _ =>
                    {
                    }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenIncompatibleEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEnumerable<TestEventTwo>>(
                    _ =>
                    {
                    }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenIncompatibleAsyncEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(_ => Task.CompletedTask));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenIncompatibleMessageStreamSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventTwo>>(_ => Task.CompletedTask));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    private record Envelope<T>(T? Message = default) : IEnvelope<T>
    {
        public Type MessageType => Message?.GetType() ?? typeof(T);

        object? IEnvelope.Message => Message;
    }

    private record TestEventOne;

    private record TestEventTwo;
}
