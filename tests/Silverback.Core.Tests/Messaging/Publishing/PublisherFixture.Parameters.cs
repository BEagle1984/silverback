// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldResolveAdditionalSubscriberParameters()
    {
        Counter counter = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddTransientSubscriber<SubscriberWithParameters>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        counter.Value.Should().Be(4);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldResolveAdditionalDelegateSubscriberParameters()
    {
        Counter counter = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne, Counter>(Handle1)
                .AddDelegateSubscriber<object, Counter>(Handle2));

        static void Handle1(TestEvent message, Counter counter) => counter.Increment();
        static void Handle2(object message, Counter counterParam) => counterParam.IncrementAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        counter.Value.Should().Be(4);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [UsedImplicitly]
    private class SubscriberWithParameters
    {
        [UsedImplicitly]
        public void SyncSubscriber(TestEventOne message, Counter counter) => counter.Increment();

        [UsedImplicitly]
        public Task AsyncSubscriber(TestEventOne message, Counter counter) => counter.IncrementAsync();
    }
}
