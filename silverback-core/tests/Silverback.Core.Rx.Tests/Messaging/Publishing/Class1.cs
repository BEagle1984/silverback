using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Core.Rx.Tests.TestTypes.Messages;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Core.Rx.Tests.Messaging.Publishing
{
    public class PublisherTests
    {
        private IPublisher GetPublisher(params ISubscriber[] subscribers) => GetPublisher(null, subscribers);

        private IPublisher GetPublisher(Action<BusConfigurator> configAction, params ISubscriber[] subscribers)
        {
            var services = new ServiceCollection();
            services.AddBus(o => o.Observable());

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            foreach (var sub in subscribers)
                services.AddSingleton<ISubscriber>(sub);

            var serviceProvider = services.BuildServiceProvider();

            configAction?.Invoke(serviceProvider.GetRequiredService<BusConfigurator>());

            return serviceProvider.GetRequiredService<IPublisher>();
        }

       [Fact]
        public void Publish_SomeMessages_ReceivedAsObservable()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((IObservable<object> _) => count++));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedAsObservable()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((IObservable<object> _) => count++));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsObservable()
        {
            int batchesCount = 0;
            int messagesCount = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((IObservable<ICommand> observable) =>
                {
                    batchesCount++;
                    observable.Subscribe(_ => messagesCount++);
                }));

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });
            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            batchesCount.Should().Be(2);
            messagesCount.Should().Be(6);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceived()
        {
            int batchesCount = 0;
            int messagesCount = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((IObservable<ICommand> observable) =>
                {
                    batchesCount++;
                    observable.Subscribe(_ => messagesCount++);
                }));

            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });
            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            batchesCount.Should().Be(2);
            messagesCount.Should().Be(6);
        }

        [Fact]
        public void Publish_NewMessagesObservableReturned_MessagesRepublished()
        {
            int count = 0;
            var publisher = GetPublisher(config =>
                    config
                        .Subscribe<TestCommandOne>((TestCommandOne msg) => new [] {new TestCommandTwo(), new TestCommandTwo()}.ToObservable())
                        .Subscribe<TestCommandTwo>((TestCommandTwo _) => count++));

            publisher.Publish(new TestCommandOne());

            count.Should().Be(2);
        }
    }
}
