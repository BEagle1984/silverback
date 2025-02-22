// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToExactType()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new SimpleSubscriber(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToExactType()
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
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEvent>(Handle));

        void Handle(TestEvent message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeDelegateSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEvent>(Handle));

        void Handle(IEvent message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestCommandOne());

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToExactType()
    {
        TestingCollection<TestEventOne> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber(new GenericSubscriber<TestEventOne>(messages)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        publisher.Publish(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToBaseType()
    {
        TestingCollection<TestEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeGenericSubscriber_WhenSubscribedToInterface()
    {
        TestingCollection<IEvent> messages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
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

        messages.Count.ShouldBe(4);
        messages[0].ShouldBeOfType<TestEventOne>();
        messages[1].ShouldBeOfType<TestEventTwo>();
        messages[2].ShouldBeOfType<TestEventOne>();
        messages[3].ShouldBeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldThrow_WhenThrowIfUnhandledIsEnabledAndMessageIsNotSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand>(Handle));

        static void Handle(ICommand message)
        {
            // Irrelevant
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne(), true);
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

        actSync.ShouldThrow<UnhandledMessageException>();
        await actAsync.ShouldThrowAsync<UnhandledMessageException>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotThrow_WhenThrowIfUnhandledIsEnabledAndMessageIsSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEvent>(Handle));

        static void Handle(IEvent message)
        {
            // Irrelevant
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync = () => publisher.Publish(new TestEventOne(), true);
        Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

        actSync.ShouldNotThrow();
        await actAsync.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotThrow_WhenThrowIfUnhandledIsDisabledAndMessageIsNotSubscribed()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand>(Handle));

        static void Handle(ICommand message)
        {
            // Irrelevant
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Action actSync1 = () => publisher.Publish(new TestEventOne());
        Action actSync2 = () => publisher.Publish(new TestEventOne());
        Func<Task> actAsync1 = () => publisher.PublishAsync(new TestEventOne());
        Func<Task> actAsync2 = () => publisher.PublishAsync(new TestEventOne());

        actSync1.ShouldNotThrow();
        actSync2.ShouldNotThrow();
        await actAsync1.ShouldNotThrowAsync();
        await actAsync2.ShouldNotThrowAsync();
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
}
