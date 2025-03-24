// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle));

        void Handle(TestEventOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscriber()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle));

        void Handle(TestEventOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Count.ShouldBe(2);
    }

    [Fact]
    public void Publish_ShouldInvokeSyncAndAsyncDelegateSubscriber()
    {
        TestingCollection<TestEventOne> syncMessages = [];
        TestingCollection<TestEventOne> asyncMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => syncMessages.Add(message);
        ValueTask Handle2(TestEventOne message) => asyncMessages.AddAsync(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventOne());

        syncMessages.Count.ShouldBe(2);
        asyncMessages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSyncAndAsyncDelegateSubscriber()
    {
        TestingCollection<TestEventOne> syncMessages = [];
        TestingCollection<TestEventOne> asyncMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => syncMessages.Add(message);
        ValueTask Handle2(TestEventOne message) => asyncMessages.AddAsync(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        syncMessages.Count.ShouldBe(2);
        asyncMessages.Count.ShouldBe(2);
    }

    [Fact]
    public void Publish_ShouldInvokeDelegateSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];
        int executingCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1, new DelegateSubscriptionOptions { IsExclusive = true })
                .AddDelegateSubscriber<TestEventOne>(Handle2, new DelegateSubscriptionOptions { IsExclusive = true }));

        Task Handle1(TestEventOne message) => ExecuteAsync(message, messages1);
        Task Handle2(TestEventOne message) => ExecuteAsync(message, messages2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            Interlocked.Increment(ref executingCount);

            if (executingCount > 1)
                throw new InvalidOperationException("Exclusive subscriber is already executing.");

            await messages.AddAsync(message);
            await Task.Delay(100);
            Interlocked.Decrement(ref executingCount);
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Count.ShouldBe(1);
        messages2.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscribersSequentially_WhenExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];
        int executingCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1, new DelegateSubscriptionOptions { IsExclusive = true })
                .AddDelegateSubscriber<TestEventOne>(Handle2, new DelegateSubscriptionOptions { IsExclusive = true }));

        Task Handle1(TestEventOne message) => ExecuteAsync(message, messages1);
        Task Handle2(TestEventOne message) => ExecuteAsync(message, messages2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            Interlocked.Increment(ref executingCount);

            if (executingCount > 1)
                throw new InvalidOperationException("Exclusive subscriber is already executing.");

            await messages.AddAsync(message);
            await Task.Delay(100);
            Interlocked.Decrement(ref executingCount);
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Count.ShouldBe(1);
        messages2.Count.ShouldBe(1);
    }

    [Fact]
    public void Publish_ShouldInvokeDelegateSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];
        CountdownEvent countdownEvent = new(2);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1, new DelegateSubscriptionOptions { IsExclusive = false })
                .AddDelegateSubscriber<TestEventOne>(Handle2, new DelegateSubscriptionOptions { IsExclusive = false }));

        Task Handle1(TestEventOne message) => ExecuteAsync(message, messages1);
        Task Handle2(TestEventOne message) => ExecuteAsync(message, messages2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            if (!countdownEvent.Signal())
                countdownEvent.WaitOrThrow();

            await messages.AddAsync(message);
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        messages1.Count.ShouldBe(1);
        messages2.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeDelegateSubscribersInParallel_WhenNotExclusive()
    {
        TestingCollection<TestEventOne> messages1 = [];
        TestingCollection<TestEventOne> messages2 = [];
        CountdownEvent countdownEvent = new(2);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1, new DelegateSubscriptionOptions { IsExclusive = false })
                .AddDelegateSubscriber<TestEventOne>(Handle2, new DelegateSubscriptionOptions { IsExclusive = false }));

        Task Handle1(TestEventOne message) => ExecuteAsync(message, messages1);
        Task Handle2(TestEventOne message) => ExecuteAsync(message, messages2);

        async Task ExecuteAsync(TestEventOne message, TestingCollection<TestEventOne> messages)
        {
            if (!countdownEvent.Signal())
                countdownEvent.WaitOrThrow();

            await messages.AddAsync(message);
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne());

        messages1.Count.ShouldBe(1);
        messages2.Count.ShouldBe(1);
    }
}
