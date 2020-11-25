// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Rx.TestTypes.Messages;
using Silverback.Tests.Core.Rx.TestTypes.Messages.Base;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Publishing
{
    public class PublisherTests
    {
        [Fact]
        public void Publish_SomeMessages_ReceivedAsObservable()
        {
            int count = 0;
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((IObservable<object> _) => count++));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedAsObservable()
        {
            int count = 0;
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((IObservable<object> _) => count++));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsObservable()
        {
            int batchesCount = 0;
            int messagesCount = 0;
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (IObservable<ICommand> observable) =>
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
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (IObservable<ICommand> observable) =>
                        {
                            batchesCount++;
                            observable.Subscribe(_ => messagesCount++);
                        }));

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });
            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            batchesCount.Should().Be(2);
            messagesCount.Should().Be(6);
        }

        [Fact]
        public void Publish_MessagesObservableReturned_MessagesRepublished()
        {
            int count = 0;
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (TestCommandOne _) =>
                            new[] { new TestCommandTwo(), new TestCommandTwo() }.ToObservable())
                    .AddDelegateSubscriber((TestCommandTwo _) => count++));

            publisher.Publish(new TestCommandOne());

            count.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_MessagesObservableReturned_MessagesRepublished()
        {
            int count = 0;
            var publisher = GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (TestCommandOne _) =>
                            new[] { new TestCommandTwo(), new TestCommandTwo() }.ToObservable())
                    .AddDelegateSubscriber((TestCommandTwo _) => count++));

            await publisher.PublishAsync(new TestCommandOne());

            count.Should().Be(2);
        }

        private static IPublisher GetPublisher(Action<ISilverbackBuilder> buildAction)
        {
            var services = new ServiceCollection();

            services.AddNullLogger();

            var builder = services.AddSilverback().AsObservable();

            buildAction(builder);

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            return serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IPublisher>();
        }
    }
}
