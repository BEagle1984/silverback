// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Core.Rx.Tests.TestTypes.Messages;
using Silverback.Core.Rx.Tests.TestTypes.Messages.Base;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Xunit;

namespace Silverback.Core.Rx.Tests.Messaging
{
    [Collection("MessageObservable")]
    public class TypedMessageObservableTests
    {
        private readonly IPublisher _publisher;
        private readonly MessageObservable<IEvent> _messageObservable;

        public TypedMessageObservableTests()
        {
            var observable = new MessageObservable();
            _messageObservable = new MessageObservable<IEvent>(observable);

            var services = new ServiceCollection();
            services.AddBus();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddSingleton<ISubscriber>(observable);

            var serviceProvider = services.BuildServiceProvider();

            _publisher = serviceProvider.GetRequiredService<IPublisher>();
        }

        [Fact]
        public void Subscribe_MessagesPublished_MessagesReceived()
        {
            int count = 0;

            _messageObservable.Subscribe(_ => count++);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestCommandOne());

            Assert.Equal(2, count);
        }

        [Fact]
        public void Subscribe_MessagesPublished_MessagesReceivedByMultipleSubscribers()
        {
            int count = 0;

            _messageObservable.Subscribe(_ => count++);
            _messageObservable.Subscribe(_ => count++);
            _messageObservable.Subscribe(_ => count++);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestCommandOne());

            Assert.Equal(3, count);
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

            Assert.Equal(2, count);
            Assert.Equal(3, threads.Distinct().Count());
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

            Assert.Equal(4, count);
            Assert.Equal(3, threads.Distinct().Count());
        }
    }
}