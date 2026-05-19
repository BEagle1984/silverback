// Copyright (c) 2026 Sergio Aquilini
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

public class SubscribedMethodsCacheTests
{
    [Fact]
    public void GetExclusiveMethods_ShouldReturnMethodsSubscribingMessageType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(_ =>
            {
            })
            .AddDelegateSubscriber<TestEventTwo>(_ =>
            {
            }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        IReadOnlyList<SubscribedMethod> methods = cache.GetExclusiveMethods(new Envelope<TestEventOne>(), serviceProvider);

        methods.Count.ShouldBe(1);
    }

    [Fact]
    public void GetExclusiveMethods_ShouldReturnMethodsSubscribingCompatibleTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IEvent>(_ =>
            {
            })
            .AddDelegateSubscriber<BaseEventOne>(_ =>
            {
            })
            .AddDelegateSubscriber<TestEventOne>(_ =>
            {
            })
            .AddDelegateSubscriber<TestEventTwo>(_ =>
            {
            }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        IReadOnlyList<SubscribedMethod> methods = cache.GetExclusiveMethods(new Envelope<TestEventOne>(), serviceProvider);

        methods.Count.ShouldBe(3);
    }

    [Fact]
    public void GetExclusiveMethods_ShouldNotReturnTombstoneHandlers_WhenNotTombstone()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(_ =>
            {
            })
            .AddDelegateSubscriber<Tombstone<TestEventOne>>(_ =>
            {
            }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        IReadOnlyList<SubscribedMethod> methods = cache.GetExclusiveMethods(new Envelope<TestEventOne>(), serviceProvider);

        methods.Count.ShouldBe(1);
        methods[0].Parameters[0].ParameterType.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnTrueForEnvelope_WhenAsyncEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestEventOne>(_ => Task.CompletedTask)
            .AddDelegateSubscriber<TestEventTwo>(_ =>
            {
            }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenIncompatibleEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IEnumerable<TestEventTwo>>(_ =>
            {
            }));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMessageStreamSubscriber_ShouldReturnFalseForEnvelope_WhenIncompatibleAsyncEnumerableSubscriberRegistered()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventTwo>>(_ => Task.CompletedTask));
        SubscribedMethodsCache cache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = cache.HasMessageStreamSubscriber(new Envelope<TestEventOne>(), serviceProvider);

        result.ShouldBeFalse();
    }

    private class Envelope<T> : IEnvelope<T>
        where T : new()
    {
        public Envelope()
            : this(new T())
        {
        }

        public Envelope(T? message)
        {
            Message = message;
        }

        public T? Message { get; }

        public Type MessageType => Message?.GetType() ?? typeof(T);

        object? IEnvelope.Message => Message;
    }

    private record IEvent;

    private record BaseEventOne : IEvent;

    private record TestEventOne : BaseEventOne;

    private record TestEventTwo : IEvent;
}
