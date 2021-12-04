// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public class SubscribedMethodsCacheTests
{
    [Fact]
    public void IsSubscribed_NoSubscribers_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_UnsubscribedType_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventTwo _) =>
                    {
                    })
                .AddDelegateSubscriber(
                    (IEnumerable<TestEventThree> _) =>
                    {
                    })
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventFour> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_Subscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventOne _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_SubscribedMoreThanOnce_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventOne _) =>
                    {
                    })
                .AddDelegateSubscriber(
                    (IEnumerable<TestEventOne> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_BaseTypeSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IEvent _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_EnvelopeMessageSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventOne _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne()));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_EnvelopeMessageBaseTypeSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (ITestMessage _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne()));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_EnvelopeNotUnwrapping_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventOne _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne(), false));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_EnvelopeMessageNotSubscribed_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (TestEventOne _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventTwo()));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_StreamNotSubscribed_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IMessage>());

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_StreamMatchingTypeNotSubscribed_FalseReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<ICommand> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IEvent>());

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscribed_StreamSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventOne> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<TestEventOne>());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_StreamBaseTypeSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<ITestMessage> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IMessage>());

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSubscribed_StreamDerivedTypeSubscribed_TrueReturned()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IMessage> _) =>
                    {
                    }));
        SubscribedMethodsCache subscribedMethodsCache = serviceProvider.GetRequiredService<SubscribedMethodsCache>();

        bool result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<ITestMessage>());

        result.Should().BeTrue();
    }
}
