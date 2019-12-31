// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.Rx.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging
{
    [Collection("MessageObservable")]
    public class MessageObservableTests
    {
        private readonly IPublisher _publisher;
        private readonly MessageObservable _messageObservable;

        public MessageObservableTests()
        {
            _messageObservable = new MessageObservable();

            var services = new ServiceCollection();
            services
                .AddSilverback()
                .AddSingletonSubscriber(_messageObservable);

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _publisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IPublisher>();
        }

        [Fact]
        public void Subscribe_MessagesPublished_MessagesReceived2()
        {
            int count = 0;

            _messageObservable.Subscribe(_ => count++);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestCommandOne());

            count.Should().Be(3);
        }

        [Fact]
        public void Subscribe_MessagesPublished_MessagesReceivedByMultipleSubscribers1()
        {
            int count = 0;

            _messageObservable.Subscribe(_ => count++);
            _messageObservable.Subscribe(_ => count++);
            _messageObservable.Subscribe(_ => count++);

            _publisher.Publish(new TestEventOne());

            count.Should().Be(3);
        }

        [Fact]
        public void Subscribe_MessagesPublishedFromMultipleThreads_MessagesReceived()
        {
            int count = 0;
            var threads = new ConcurrentBag<int>();

            _messageObservable.Subscribe(_ => count++);

            Parallel.Invoke(
                () =>
                {
                    _publisher.Publish(new TestEventOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                },
                () =>
                {
                    _publisher.Publish(new TestCommandOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                },
                () =>
                {
                    _publisher.Publish(new TestEventOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                });

            count.Should().Be(3);
            threads.Distinct().Count().Should().BeGreaterThan(1);
        }

        [Fact]
        public async Task Subscribe_MessagesPublishedFromMultipleThreads_MessagesReceivedInMultipleThreads()
        {
            int count = 0;
            var threads = new ConcurrentBag<int>();

            _messageObservable.ObserveOn(NewThreadScheduler.Default).Subscribe(_ => count++);
            _messageObservable.ObserveOn(NewThreadScheduler.Default).Subscribe(_ => count++);

            Parallel.Invoke(
                () =>
                {
                    _publisher.Publish(new TestEventOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                },
                () =>
                {
                    _publisher.Publish(new TestCommandOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                },
                () =>
                {
                    _publisher.Publish(new TestEventOne());
                    threads.Add(Thread.CurrentThread.ManagedThreadId);
                });

            await Task.Delay(100);

            count.Should().Be(6);
            threads.Distinct().Count().Should().BeGreaterThan(1);
        }
    }
}