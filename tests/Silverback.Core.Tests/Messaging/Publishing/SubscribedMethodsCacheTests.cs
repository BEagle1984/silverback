// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class SubscribedMethodsCacheTests
    {
        [Fact]
        public void IsSubscribed_NoSubscribers_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback());
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_UnsubscribedType_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventTwo _) => { })
                    .AddDelegateSubscriber((IEnumerable<TestEventThree> _) => { })
                    .AddDelegateSubscriber((IMessageStreamEnumerable<TestEventFour> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_Subscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_SubscribedMoreThanOnce_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => { })
                    .AddDelegateSubscriber((IEnumerable<TestEventOne> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_BaseTypeSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IEvent _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEventOne());

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_EnvelopeMessageSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne()));

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_EnvelopeMessageBaseTypeSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((ITestMessage _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne()));

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_EnvelopeNotUnwrapping_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventOne(), false));

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_EnvelopeMessageNotSubscribed_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new TestEnvelope(new TestEventTwo()));

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_StreamNotSubscribed_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback());
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IMessage>());

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_StreamMatchingTypeNotSubscribed_FalseReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IMessageStreamEnumerable<ICommand> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IEvent>());

            result.Should().BeFalse();
        }

        [Fact]
        public void IsSubscribed_StreamSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IMessageStreamEnumerable<TestEventOne> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<TestEventOne>());

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_StreamBaseTypeSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IMessageStreamEnumerable<ITestMessage> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<IMessage>());

            result.Should().BeTrue();
        }

        [Fact]
        public void IsSubscribed_StreamDerivedTypeSubscribed_TrueReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IMessageStreamEnumerable<IMessage> _) => { }));
            var subscribedMethodsCache = serviceProvider.GetRequiredService<ISubscribedMethodsCache>();

            var result = subscribedMethodsCache.IsSubscribed(new MessageStreamProvider<ITestMessage>());

            result.Should().BeTrue();
        }
    }
}
