// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    private interface ICustomWrapper<T> : IMessageWrapper<T>
    {
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldPublishEnvelope()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEnvelope>(Handle));

        void Handle(IEnvelope envelope) => messages.Add(envelope);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<TestEnvelope>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldUnwrapEnvelope()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestCommandOne>(Handle));

        void Handle(TestCommandOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<TestCommandOne>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldPublishCustomWrapper()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<CustomWrapper<TestCommandOne>>(Handle));

        void Handle(CustomWrapper<TestCommandOne> wrapper) => messages.Add(wrapper);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new CustomWrapper<TestCommandOne>(new TestCommandOne()));
        await publisher.PublishAsync(new CustomWrapper<TestCommandOne>(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<CustomWrapper<TestCommandOne>>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldMatchCustomWrapperInterface()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICustomWrapper<TestCommandOne>>(Handle));

        void Handle(ICustomWrapper<TestCommandOne> wrapper) => messages.Add(wrapper);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new CustomWrapper<TestCommandOne>(new TestCommandOne()));
        await publisher.PublishAsync(new CustomWrapper<TestCommandOne>(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<CustomWrapper<TestCommandOne>>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldUnwrapCustomWrapper()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestCommandOne>(Handle));

        void Handle(TestCommandOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new CustomWrapper<TestCommandOne>(new TestCommandOne()));
        await publisher.PublishAsync(new CustomWrapper<TestCommandOne>(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<TestCommandOne>();
    }

    // This test simulates the case where the IRawInboundEnvelope isn't really an IEnvelope
    [Fact]
    public async Task PublishAndPublishAsync_ShouldCastEnvelope()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ITestRawEnvelope>(Handle));

        void Handle(ITestRawEnvelope envelope) => messages.Add(envelope);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Should().HaveCount(2);
        messages.Should().AllBeOfType<TestEnvelope>();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotUnwrap_WhenUnwrappingIsDisabled()
    {
        List<object> messages = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand>(Handle1)
                .AddDelegateSubscriber<IEnvelope>(Handle2));

        void Handle1(ICommand message) => messages.Add(message);
        void Handle2(IEnvelope envelope) => messages.Add(envelope);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne(), false));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne(), false));

        messages.OfType<TestEnvelope>().Should().HaveCount(2);
        messages.OfType<TestCommandOne>().Should().BeEmpty();
    }

    private class CustomWrapper<T> : ICustomWrapper<T>
    {
        public CustomWrapper(T? message)
        {
            Message = message;
        }

        public T? Message { get; }
    }
}
