// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
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
    public void Publish_ShouldRethrow_WhenSyncSubscriberThrows()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => messages.Add(message);
        static void Handle2(TestEventOne message) => throw new InvalidOperationException("test");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action act = () => publisher.Publish(new TestEventOne());

        act.Should().Throw<TargetInvocationException>();
        messages.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_ShouldRethrow_WhenSyncSubscriberThrows()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => messages.Add(message);
        static void Handle2(TestEventOne message) => throw new InvalidOperationException("test");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(new TestEventOne()).AsTask();

        await act.Should().ThrowAsync<TargetInvocationException>();
        messages.Should().HaveCount(1);
    }

    [Fact]
    public void Publish_ShouldRethrow_WhenAsyncSubscriberThrows()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => messages.Add(message);
        static Task Handle2(TestEventOne message) => throw new InvalidOperationException("test");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action act1 = () => publisher.Publish(new TestEventOne());
        Action act2 = () => publisher.Publish(new TestEventOne());

        act1.Should().Throw<TargetInvocationException>();
        act2.Should().Throw<TargetInvocationException>();
        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldRethrow_WhenAsyncSubscriberThrows()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne>(Handle2));

        void Handle1(TestEventOne message) => messages.Add(message);
        static Task Handle2(TestEventOne message) => throw new InvalidOperationException("test");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Func<Task> act1 = () => publisher.PublishAsync(new TestEventOne()).AsTask();
        Func<Task> act2 = () => publisher.PublishAsync(new TestEventOne()).AsTask();

        await act1.Should().ThrowAsync<TargetInvocationException>();
        await act2.Should().ThrowAsync<TargetInvocationException>();
        messages.Should().HaveCount(2);
    }
}
