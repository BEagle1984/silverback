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
    private interface IService
    {
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToExactType()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToExactType()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<TestEventOne>(Handle));

        void Handle(TestEventOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new BaseTypeSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<TestEvent>(Handle));

        void Handle(TestEvent message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new InterfaceSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<IEvent>(Handle));

        void Handle(IEvent message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToExactType()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new GenericSubscriber<TestEventOne>(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new GenericSubscriber<TestEvent>(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new GenericSubscriber<IEvent>(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Should().HaveCount(4);
        messages[0].Should().BeOfType<TestEventOne>();
        messages[1].Should().BeOfType<TestEventTwo>();
        messages[2].Should().BeOfType<TestEventOne>();
        messages[3].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribers_WhenRegisteredViaInterface()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSingleton<IService>(new ServiceSubscriber(messages))
                .AddSingleton(new ServiceSubscriber(messages))
                .AddSilverback()
                .AddSubscribers<IService>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldThrow_WhenSubscriberRegisteredViaInterfaceIsNotRegisteredAsSelf()
    {
        TestingCollection<TestEventOne> messages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSingleton<IService>(new ServiceSubscriber(messages))
                .AddSilverback()
                .AddSubscribers<IService>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne());
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne());

        actSync.Should().Throw<InvalidOperationException>().WithMessage("No service for type *");
        await actAsync.Should().ThrowAsync<InvalidOperationException>().WithMessage("No service for type *");

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldThrow_WhenThrowIfUnhandledIsEnabledAndMessageIsNotSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<ICommand>(Handle));

        static void Handle(ICommand message)
        {
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne(), true);
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

        actSync.Should().ThrowExactly<AggregateException>();
        await actAsync.Should().ThrowExactlyAsync<UnhandledMessageException>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotThrow_WhenThrowIfUnhandledIsEnabledAndMessageIsSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<IEvent>(Handle));

        static void Handle(IEvent message)
        {
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne(), true);
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

        actSync.Should().NotThrow();
        await actAsync.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotThrow_WhenThrowIfUnhandledIsDisabledAndMessageIsNotSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<ICommand>(Handle));

        static void Handle(ICommand message)
        {
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync1 = () => publisher.Publish(new TestEventOne(), false);
        Action actSync2 = () => publisher.Publish(new TestEventOne());
        Func<Task> actAsync1 = () => publisher.PublishAsync(new TestEventOne(), false);
        Func<Task> actAsync2 = () => publisher.PublishAsync(new TestEventOne());

        actSync1.Should().NotThrow();
        actSync2.Should().NotThrow();
        await actAsync1.Should().NotThrowAsync();
        await actAsync2.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_SubscribedMessageWithThrowIfUnhandled_NoExceptionThrown()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<IEvent>(Handle));

        static void Handle(IEvent message)
        {
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne(), true);
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

        actSync.Should().NotThrow();
        await actAsync.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_NotSubscribedMessageWithoutThrowIfUnhandled_NoExceptionThrown()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber2<IEvent>(Handle));

        static void Handle(IEvent message)
        {
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestCommandOne());
        Func<Task> actAsync = () => publisher.PublishAsync(new TestCommandOne());

        actSync.Should().NotThrow();
        await actAsync.Should().NotThrowAsync();
    }

    private class BaseTypeSubscriber
    {
        private readonly TestingCollection<TestEvent> _messages;

        public BaseTypeSubscriber(TestingCollection<TestEvent> messages)
        {
            _messages = messages;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void Subscriber(TestEvent message) => _messages.Add(message);
    }

    private class InterfaceSubscriber
    {
        private readonly TestingCollection<IEvent> _messages;

        public InterfaceSubscriber(TestingCollection<IEvent> messages)
        {
            _messages = messages;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void Subscriber(IEvent message) => _messages.Add(message);
    }

    private class GenericSubscriber<TMessage>
    {
        private readonly TestingCollection<TMessage> _messages;

        public GenericSubscriber(TestingCollection<TMessage> messages)
        {
            _messages = messages;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void Subscriber(TMessage message) => _messages.Add(message);
    }

    private class ServiceSubscriber : IService
    {
        private readonly TestingCollection<TestEventOne> _messages;

        public ServiceSubscriber(TestingCollection<TestEventOne> messages)
        {
            _messages = messages;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void Subscriber(TestEventOne message) => _messages.Add(message);
    }
}
