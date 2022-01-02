// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public void Publish_ShouldInvokeDelegateSubscriber()
    {
        TestingCollection<TestEventOne> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(message => messages.Add(message)));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscriber()
    {
        TestingCollection<TestEventOne> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(message => messages.Add(message)));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public void Publish_ShouldInvokeSyncAndAsyncDelegateSubscriber()
    {
        TestingCollection<TestEventOne> syncMessages = new();
        TestingCollection<TestEventOne> asyncMessages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(message => syncMessages.Add(message))
                .AddDelegateSubscriber<TestEventOne>(message => asyncMessages.AddAsync(message).AsTask()));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        syncMessages.Should().HaveCount(2);
        asyncMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSyncAndAsyncDelegateSubscriber()
    {
        TestingCollection<TestEventOne> syncMessages = new();
        TestingCollection<TestEventOne> asyncMessages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(message => syncMessages.Add(message))
                .AddDelegateSubscriber<TestEventOne>(message => asyncMessages.AddAsync(message).AsTask()));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        syncMessages.Should().HaveCount(2);
        asyncMessages.Should().HaveCount(2);
    }

    [Fact]
    public void Publish_ShouldInvokeDelegateSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = new();
        TestingCollection<TestEventOne> messages2 = new();
        int executingCount = 0;

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            Interlocked.Increment(ref executingCount);

            if (executingCount > 1)
                throw new InvalidOperationException("Exclusive subscriber is already executing.");

            await messages.AddAsync(message);
            await Task.Delay(100);
            Interlocked.Decrement(ref executingCount);
        }

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages1),
                    new DelegateSubscriptionOptions { Exclusive = true })
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages2),
                    new DelegateSubscriptionOptions { Exclusive = true }));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = new();
        TestingCollection<TestEventOne> messages2 = new();
        int executingCount = 0;

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            Interlocked.Increment(ref executingCount);

            if (executingCount > 1)
                throw new InvalidOperationException("Exclusive subscriber is already executing.");

            await messages.AddAsync(message);
            await Task.Delay(100);
            Interlocked.Decrement(ref executingCount);
        }

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages1),
                    new DelegateSubscriptionOptions { Exclusive = true })
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages2),
                    new DelegateSubscriptionOptions { Exclusive = true }));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public void Publish_ShouldInvokeDelegateSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = new();
        TestingCollection<TestEventOne> messages2 = new();
        CountdownEvent countdownEvent = new(2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            if (!countdownEvent.Signal())
            {
                if (!countdownEvent.Wait(TimeSpan.FromSeconds(5)))
                    throw new InvalidOperationException("Not exclusive subscribers are not executing in parallel.");
            }

            await messages.AddAsync(message);
        }

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages1),
                    new DelegateSubscriptionOptions { Exclusive = false })
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages2),
                    new DelegateSubscriptionOptions { Exclusive = false }));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = new();
        TestingCollection<TestEventOne> messages2 = new();
        CountdownEvent countdownEvent = new(2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            if (!countdownEvent.Signal())
            {
                if (!countdownEvent.Wait(TimeSpan.FromSeconds(5)))
                    throw new InvalidOperationException("Not exclusive subscribers are not executing in parallel.");
            }

            await messages.AddAsync(message);
        }

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages1),
                    new DelegateSubscriptionOptions { Exclusive = false })
                .AddDelegateSubscriber<TestEventOne>(
                    message => ExecuteAsync(message, messages2),
                    new DelegateSubscriptionOptions { Exclusive = false }));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Should().HaveCount(1);
        messages2.Should().HaveCount(1);
    }
}
