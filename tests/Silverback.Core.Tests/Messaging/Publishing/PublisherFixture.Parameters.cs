// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
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
                .AddDelegateSubscriber<TestEventOne, Counter>((_, counterParam) => counterParam.Increment())
                .AddDelegateSubscriber((object _, Counter counterParam) => counterParam.IncrementAsync()));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        counter.Value.Should().Be(4);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class SubscriberWithParameters
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public void SyncSubscriber(TestEventOne message, Counter counter) => counter.Increment();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task AsyncSubscriber(TestEventOne message, Counter counter) => counter.IncrementAsync();
    }
}
