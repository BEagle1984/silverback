// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
    public void Publish_ShouldResolveAdditionalArguments()
    {
        Counter counter = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddTransientSubscriber(_ => new SubscriberWithParameters(CancellationToken.None)));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        counter.Value.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldResolveAdditionalArguments()
    {
        Counter counter = new();
        CancellationToken cancellationToken = new(false);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddTransientSubscriber(_ => new SubscriberWithParameters(cancellationToken)));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne(), cancellationToken);

        counter.Value.ShouldBe(2);
    }

    [Fact]
    public void Publish_ShouldResolveAdditionalDelegateSubscriberArguments()
    {
        Counter counter = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne, Counter>(Handle1)
                .AddDelegateSubscriber<object, Counter>(Handle2));

        static void Handle1(TestEvent message, Counter counter) => counter.Increment();
        static Task Handle2(object message, Counter counterParam) => counterParam.IncrementAsync();

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());

        counter.Value.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldResolveAdditionalDelegateSubscriberArguments()
    {
        Counter counter = new();
        CancellationToken cancellationTokenArgument = new(false);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddSingleton(counter)
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestEventOne, Counter>(Handle1)
                .AddDelegateSubscriber<object, Counter, CancellationToken>(Handle2));

        static void Handle1(TestEvent message, Counter counter) => counter.Increment();

        Task Handle2(object message, Counter counterParam, CancellationToken cancellationToken)
        {
            if (cancellationToken != cancellationTokenArgument)
                throw new ArgumentException("The passed cancellationToken doesn't match with the expected one.");

            return counterParam.IncrementAsync();
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestEventOne(), cancellationTokenArgument);

        counter.Value.ShouldBe(2);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class SubscriberWithParameters
    {
        private readonly CancellationToken _expectedCancellationToken;

        public SubscriberWithParameters(CancellationToken expectedCancellationToken)
        {
            _expectedCancellationToken = expectedCancellationToken;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Test code")]
        public void SyncSubscriber(TestEventOne message, Counter counter, CancellationToken cancellationToken)
        {
            if (cancellationToken != _expectedCancellationToken)
                throw new ArgumentException("The passed cancellationToken doesn't match with the expected one.");

            counter.Increment();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Test code")]
        public Task AsyncSubscriber(TestEventOne message, Counter counter, CancellationToken cancellationToken)
        {
            if (cancellationToken != _expectedCancellationToken)
                throw new ArgumentException("The passed cancellationToken doesn't match with the expected one.");

            return counter.IncrementAsync();
        }
    }
}
