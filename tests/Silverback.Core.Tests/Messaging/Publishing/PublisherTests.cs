// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Core.TestTypes;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    public class PublisherTests
    {
        private readonly TestSubscriber _syncSubscriber;
        private readonly TestAsyncSubscriber _asyncSubscriber;
        private readonly TestEnumerableSubscriber _syncEnumerableSubscriber;
        private readonly TestAsyncEnumerableSubscriber _asyncEnumerableSubscriber;
        private readonly TestReadOnlyCollectionSubscriber _syncReadOnlyCollectionSubscriber;
        private readonly TestAsyncReadOnlyCollectionSubscriber _asyncReadOnlyCollectionSubscriber;
        private readonly TestFilteredSubscriber _filteredSubscriber;

        public PublisherTests()
        {
            _syncSubscriber = new TestSubscriber();
            _asyncSubscriber = new TestAsyncSubscriber();
            _syncEnumerableSubscriber = new TestEnumerableSubscriber();
            _asyncEnumerableSubscriber = new TestAsyncEnumerableSubscriber();
            _syncReadOnlyCollectionSubscriber = new TestReadOnlyCollectionSubscriber();
            _asyncReadOnlyCollectionSubscriber = new TestAsyncReadOnlyCollectionSubscriber();
            _filteredSubscriber = new TestFilteredSubscriber();
        }

        #region GetPublisher

        private IPublisher GetPublisher(params ISubscriber[] subscribers) =>
            GetPublisher(null, subscribers);

        private IPublisher GetPublisher(Action<BusConfigurator> configAction, params ISubscriber[] subscribers) =>
            GetPublisher(configAction, null, subscribers);

        private IPublisher GetPublisher(
            Action<BusConfigurator> configAction,
            IBehavior[] behaviors,
            params ISubscriber[] subscribers) =>
            GetPublisher(builder =>
                {
                    if (behaviors != null)
                    {
                        foreach (var behavior in behaviors)
                            builder.AddSingletonBehavior(behavior);
                    }

                    foreach (var sub in subscribers)
                    {
                        builder.AddSingletonSubscriber(sub.GetType(), sub);
                    }
                },
                configAction);

        private IPublisher GetPublisher(
            Action<ISilverbackBuilder> buildAction,
            Action<BusConfigurator> configAction = null) =>
            GetServiceProvider(buildAction, configAction).GetRequiredService<IPublisher>();

        private IServiceProvider GetServiceProvider(
            Action<ISilverbackBuilder> buildAction,
            Action<BusConfigurator> configAction = null)
        {
            var services = new ServiceCollection();
            var builder = services.AddSilverback();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            buildAction(builder);

            var serviceProvider = services.BuildServiceProvider();

            configAction?.Invoke(serviceProvider.GetRequiredService<BusConfigurator>());

            return serviceProvider;
        }

        #endregion

        [Fact]
        public void Publish_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((object _) => count++));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((object _) => count++));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe<object>((msg, sp) => sp != null ? count++ : count = 0));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe<object>((msg, sp) => sp != null ? count++ : count = 0));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishSyncAndAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task Publish_SomeMessages_ReceivedByAllSubscribedMethods()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), service1, service2);

            await publisher.PublishAsync(new TestCommandOne()); // service1 +2
            await publisher.PublishAsync(new TestCommandTwo()); // service2 +2
            publisher.Publish(new TestCommandOne()); // service1 +2
            await publisher.PublishAsync(new TransactionCompletedEvent()); // service1/2 +1
            publisher.Publish(new TransactionAbortedEvent()); // service1/2 +1

            service1.ReceivedMessagesCount.Should().Be(6);
            service2.ReceivedMessagesCount.Should().Be(4);
        }

        [Theory, MemberData(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public void Publish_SubscribedMessage_ReceivedRepublishedMessages(
            IEvent message,
            int expectedEventOne,
            int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            publisher.Publish(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Theory, MemberData(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(
            IEvent message,
            int expectedEventOne,
            int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            await publisher.PublishAsync(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static IEnumerable<object[]> Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData =>
            new List<object[]>
            {
                new object[] { new TestEventOne(), 1, 0 },
                new object[] { new TestEventTwo(), 1, 1 }
            };

        [Fact]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Action act1 = () => publisher.Publish(new TestEventOne());
            Action act2 = () => publisher.Publish(new TestEventTwo());

            act1.Should().Throw<AggregateException>();
            act2.Should().Throw<AggregateException>();
        }

        [Fact]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Func<Task> act1 = async () => await publisher.PublishAsync(new TestEventOne());
            Func<Task> act2 = async () => await publisher.PublishAsync(new TestEventTwo());

            act1.Should().Throw<TargetInvocationException>();
            act2.Should().Throw<TargetInvocationException>();
        }

        [Fact]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public void Publish_HandlersReturnValueOfWrongType_EmptyResultReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = publisher.Publish<int>(new TestRequestCommandOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public void Publish_SomeHandlersReturnValueOfWrongType_ValuesOfCorrectTypeAreReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier(),
                new TestRequestReplierWithWrongResponseType());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public void Publish_HandlersReturnNull_EmptyResultReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false),
                new TestRequestReplierReturningNull());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValueOfWrongType_EmptyResultReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_SomeHandlersReturnValueOfWrongType_ValuesOfCorrectTypeAreReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier(),
                new TestRequestReplierWithWrongResponseType());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnNull_EmptyResultReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false),
                new TestRequestReplierReturningNull());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public void Publish_HandlersReturnValue_EnumerableReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = publisher.Publish<IEnumerable<string>>(new TestRequestCommandTwo());

            results.SelectMany(x => x).Should().Equal("one", "two");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_EnumerableReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(new TestRequestCommandTwo());

            results.SelectMany(x => x).Should().Equal("one", "two");
        }


        [Fact]
        public void Publish_HandlersReturnValue_EmptyEnumerableReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = publisher.Publish<IEnumerable<string>>(new TestRequestCommandThree());

            results.First().Should().NotBeNull();
            results.First().Should().BeEmpty();
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_EmptyEnumerableReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(new TestRequestCommandThree());

            results.First().Should().NotBeNull();
            results.First().Should().BeEmpty();
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedAsReadOnlyCollection()
        {
            var publisher = GetPublisher(_syncReadOnlyCollectionSubscriber, _asyncReadOnlyCollectionSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedAsReadOnlyCollection()
        {
            var publisher = GetPublisher(_syncReadOnlyCollectionSubscriber, _asyncReadOnlyCollectionSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_MessagesBatch_EachMessageReceived()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_EachMessageReceived()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new ICommand[]
                { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            await publisher.PublishAsync(new ICommand[]
                { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsReadOnlyCollection()
        {
            var publisher = GetPublisher(_syncReadOnlyCollectionSubscriber, _asyncReadOnlyCollectionSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedAsReadOnlyCollection()
        {
            var publisher = GetPublisher(_syncReadOnlyCollectionSubscriber, _asyncReadOnlyCollectionSubscriber);

            await publisher.PublishAsync(new ICommand[]
                { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_ResultsReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(new[]
                { new TestRequestCommandTwo(), new TestRequestCommandTwo() });

            results.SelectMany(x => x).Should().Equal("one", "two", "one", "two");
        }

        [Fact]
        public void Publish_MessagesBatch_ResultsReturned()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), new TestRequestReplier());

            var results = publisher.Publish<IEnumerable<string>>(new[]
                { new TestRequestCommandTwo(), new TestRequestCommandTwo() });

            results.SelectMany(x => x).Should().Equal("one", "two", "one", "two");
        }

        [Fact]
        public void Publish_MessagesBatch_EachMessageReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe((ITestMessage msg) => receivedMessagesCount++)
                    .Subscribe(async (ITestMessage msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount++;
                    }));

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_EachMessageReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe((ITestMessage msg) => receivedMessagesCount++)
                    .Subscribe(async (ITestMessage msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount++;
                    }));

            await publisher.PublishAsync(new ICommand[]
                { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                    .Subscribe(async (IEnumerable<ITestMessage> msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount += msg.Count();
                    }));

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                    .Subscribe(async (IEnumerable<ITestMessage> msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount += msg.Count();
                    }));

            await publisher.PublishAsync(new ICommand[]
                { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_NewMessagesReturnedByDelegateSubscription_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(config =>
                    config
                        .Subscribe((TestCommandTwo msg) => new TestCommandOne())
                        .Subscribe(async (TestCommandTwo msg) =>
                        {
                            await Task.Delay(1);
                            return new TestCommandOne();
                        }),
                subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_ExclusiveSubscribers_SequentiallyInvoked()
        {
            var subscriber = new ExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public async Task PublishAsync_ExclusiveSubscribers_SequentiallyInvoked()
        {
            var subscriber = new ExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact, Trait("CI", "false")]
        public async Task PublishAsync_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public void Publish_ExclusiveDelegateSubscription_SequentiallyInvoked()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork())
                    .Subscribe(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Exclusive = true }));

            publisher.Publish(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_NonExclusiveDelegateSubscription_InvokedInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Exclusive = false })
                    .Subscribe(
                        (ICommand _) => parallel.DoWorkAsync(),
                        new SubscriptionOptions { Exclusive = false }));

            publisher.Publish(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public void Publish_NonParallelSubscriber_SequentiallyProcessing()
        {
            var subscriber = new NonParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
            });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2, 3, 4);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_ParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new ParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
            });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 3);
        }

        [Fact]
        public void Publish_NonParallelDelegateSubscription_SequentiallyProcessing()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Parallel = false })
                    .Subscribe(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Parallel = false }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
            });

            parallel.Steps.Should().BeEquivalentTo(1, 2, 3, 4);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_ParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Parallel = true })
                    .Subscribe(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Parallel = true }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
            });

            parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 3);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
            });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 3);
        }

        [Fact, Trait("CI", "false")]
        public async Task PublishAsync_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
            });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 4, 4, 6);
        }

        [Fact, Trait("CI", "false")]
        public void Publish_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                    .Subscribe(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
            });

            parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 4, 4, 6);
        }

        [Fact, Trait("CI", "false")]
        public async Task PublishAsync_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                    .Subscribe(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            await publisher.PublishAsync(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
            });

            parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 4, 4, 6);
        }

        [Fact]
        public void Publish_WithBehaviors_MessagesReceived()
        {
            var publisher = GetPublisher(null, new[] { new TestBehavior(), new TestBehavior() }, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_WithBehaviors_MessagesReceived()
        {
            var publisher = GetPublisher(null, new[] { new TestBehavior(), new TestBehavior() }, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_WithBehaviors_BehaviorsExecuted()
        {
            var behavior1 = new TestBehavior();
            var behavior2 = new TestBehavior();

            var publisher = GetPublisher(null, new[] { behavior1, behavior2 }, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            behavior1.EnterCount.Should().Be(2);
            behavior2.EnterCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_WithBehaviors_BehaviorsExecuted()
        {
            var behavior1 = new TestBehavior();
            var behavior2 = new TestBehavior();

            var publisher = GetPublisher(null, new[] { behavior1, behavior2 }, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            behavior1.EnterCount.Should().Be(2);
            behavior2.EnterCount.Should().Be(2);
        }

        [Fact]
        public void Publish_WithSortedBehaviors_BehaviorsExecutedInExpectedOrder()
        {
            var callsSequence = new List<string>();
            var behavior1 = new TestSortedBehavior(100, callsSequence);
            var behavior2 = new TestSortedBehavior(50, callsSequence);
            var behavior3 = new TestSortedBehavior(-50, callsSequence);
            var behavior4 = new TestSortedBehavior(-100, callsSequence);
            var behavior5 = new TestBehavior(callsSequence);

            var publisher = GetPublisher(null,
                new IBehavior[] { behavior1, behavior2, behavior3, behavior4, behavior5 }, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());

            callsSequence.Should().BeEquivalentTo(
                new[] { "-100", "-50", "unsorted", "50", "100" },
                opt => opt.WithStrictOrdering());
        }

        [Fact]
        public async Task PublishAsync_WithSortedBehaviors_BehaviorsExecuted()
        {
            var callsSequence = new List<string>();
            var behavior1 = new TestSortedBehavior(100, callsSequence);
            var behavior2 = new TestSortedBehavior(50, callsSequence);
            var behavior3 = new TestSortedBehavior(-50, callsSequence);
            var behavior4 = new TestSortedBehavior(-100, callsSequence);
            var behavior5 = new TestBehavior(callsSequence);

            var publisher = GetPublisher(null,
                new IBehavior[] { behavior1, behavior2, behavior3, behavior4, behavior5 }, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());

            callsSequence.Should().BeEquivalentTo(
                new[] { "-100", "-50", "unsorted", "50", "100" },
                opt => opt.WithStrictOrdering());
        }

        [Fact]
        public void Publish_MessageChangingBehavior_BehaviorExecuted()
        {
            var behavior = new ChangeMessageBehavior<TestCommandOne>(_ => new[]
                { new TestCommandTwo(), new TestCommandTwo(), new TestCommandTwo() });

            var publisher = GetPublisher(null, new[] { behavior }, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(4);
        }

        [Fact]
        public async Task PublishAsync_MessageChangingBehavior_BehaviorExecuted()
        {
            var behavior = new ChangeMessageBehavior<TestCommandOne>(_ => new[]
                { new TestCommandTwo(), new TestCommandTwo(), new TestCommandTwo() });

            var publisher = GetPublisher(null, new[] { behavior }, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(4);
        }

        [Fact]
        public void Publish_AutoSubscribing_AllMethodsInvoked()
        {
            var publisher = GetPublisher(_syncSubscriber);

            publisher.Publish(new TestEventOne());

            _syncSubscriber.ReceivedCallsCount.Should().Be(3);
        }

        [Fact]
        public void Publish_AutoSubscribingDisabled_OnlyDecoratedMethodsInvoked()
        {
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), _syncSubscriber);

            publisher.Publish(new TestEventOne());

            _syncSubscriber.ReceivedCallsCount.Should().Be(2);
        }

        [Fact]
        public void Publish_GenericSubscriber_MessageReceived()
        {
            var subscriber = new EventOneGenericSubscriber();
            var publisher = GetPublisher(config => config.Subscribe<ISubscriber>(false), subscriber);

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameter_MethodInvokedForMatchingMessages()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((TestEventOne x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameter_MethodInvokedForMatchingMessages()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((TestEventOne x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameterOfAncestorType_MethodInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((IEvent x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameterOfAncestorType_MethodInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((IEvent x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(3);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameterOfNotMatchingType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((ICommand x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameterOfNotMatchingType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((ICommand x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public void Publish_SubscriberWithEnumerableParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var publisher = GetPublisher(config =>
                config.Subscribe((IEnumerable<TestEventOne> x) => receivedMessages += x.Count()));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithEnumerableParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var publisher = GetPublisher(config =>
                config.Subscribe((IEnumerable<TestEventOne> x) => receivedMessages += x.Count()));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithEnumerableParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var publisher = GetPublisher(config =>
                config.Subscribe((IEnumerable<IEvent> x) => receivedMessages += x.Count()));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithEnumerableParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var publisher = GetPublisher(config =>
                config.Subscribe((IEnumerable<IEvent> x) => receivedMessages += x.Count()));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(3);
        }

        [Fact]
        public void Publish_NoMessagesMatchingEnumerableType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((IEnumerable<ICommand> x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_NoMessagesMatchingEnumerableType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = GetPublisher(config => config.Subscribe((IEnumerable<ICommand> x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public void Publish_MultipleRegisteredSubscribers_OnlyMatchingSubscribersAreResolved()
        {
            var resolved = 0;

            var serviceProvider = GetServiceProvider(builder => builder
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceOne();
                })
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceTwo();
                }));

            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .Publish(new object[] { new TestCommandOne() });
            }

            resolved.Should().Be(3); // The first time it is allowed to resolve all subscribers

            for (var i = 0; i < 3; i++)
            {
                resolved = 0;

                using (var scope = serviceProvider.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<IPublisher>()
                        .Publish(new object[] { new TestCommandOne() });
                }

                resolved.Should().Be(1); // From the second time we expect everything to be cached
            }
        }

        [Fact]
        public async Task
            PublishAsync_MultipleRegisteredSubscribers_OnlyMatchingSubscribersAreResolvedAfterFirstPublish()
        {
            var resolved = 0;

            var serviceProvider = GetServiceProvider(builder => builder
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceOne();
                })
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceTwo();
                }));

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new object[] { new TestCommandOne() });
            }

            resolved.Should().Be(3); // The first time it is allowed to resolve all subscribers

            for (var i = 0; i < 3; i++)
            {
                resolved = 0;

                using (var scope = serviceProvider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<IPublisher>()
                        .PublishAsync(new object[] { new TestCommandOne() });
                }

                resolved.Should().Be(1); // From the second time we expect everything to be cached
            }
        }

        [Fact]
        public async Task PublishAsync_MultipleRegisteredSubscribersWithPreloading_OnlyMatchingSubscribersAreResolved()
        {
            var resolved = 0;

            var serviceProvider = GetServiceProvider(builder => builder
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceOne();
                })
                .AddScopedSubscriber(_ =>
                {
                    resolved++;
                    return new TestServiceTwo();
                }));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .ScanSubscribers();

            resolved.Should().Be(2);

            for (var i = 0; i < 3; i++)
            {
                resolved = 0;

                using (var scope = serviceProvider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<IPublisher>()
                        .PublishAsync(new object[] { new TestCommandOne() });
                }

                resolved.Should().Be(1);
            }
        }

        [Fact]
        public void Publish_SubscriberNotRegisteredAsSelf_InvalidOperationExceptionIsThrown()
        {
            var publisher = GetPublisher(builder => builder
                .Services.AddScoped<ISubscriber>(_ => new TestServiceOne()));

            Action act = () => publisher.Publish(new object[] { new TestCommandOne() });

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Publish_Envelope_EnvelopeAndUnwrappedReceived()
        {
            var messages = new List<object>();
            var publisher = GetPublisher(builder => builder
                .Subscribe((object message) => messages.Add(message))
                .Subscribe((IEnvelope envelope) => messages.Add(envelope)));

            publisher.Publish(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Count().Should().Be(1);
            messages.OfType<TestCommandOne>().Count().Should().Be(1);
        }

        [Fact]
        public async Task PublishAsync_Envelope_EnvelopeAndUnwrappedReceived()
        {
            var messages = new List<object>();
            var publisher = GetPublisher(builder => builder
                .Subscribe((object message) => messages.Add(message))
                .Subscribe((IEnvelope envelope) => messages.Add(envelope)));

            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Count().Should().Be(1);
            messages.OfType<TestCommandOne>().Count().Should().Be(1);
        }

        [Fact]
        public void Publish_EnvelopeWithoutAutoUnwrap_EnvelopeOnlyIsReceived()
        {
            var messages = new List<object>();
            var publisher = GetPublisher(builder => builder
                .Subscribe((object message) => messages.Add(message))
                .Subscribe((IEnvelope envelope) => messages.Add(envelope)));

            publisher.Publish(new TestEnvelope(new TestCommandOne(), false));

            messages.OfType<TestEnvelope>().Count().Should().Be(1);
            messages.OfType<TestCommandOne>().Count().Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_EnvelopeWithoutAutoUnwrap_EnvelopeOnlyIsReceived()
        {
            var messages = new List<object>();
            var publisher = GetPublisher(builder => builder
                .Subscribe((object message) => messages.Add(message))
                .Subscribe((IEnvelope envelope) => messages.Add(envelope)));

            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne(), false));

            messages.OfType<TestEnvelope>().Count().Should().Be(1);
            messages.OfType<TestCommandOne>().Count().Should().Be(0);
        }

        [Fact]
        public async Task Publish_MessagesWithFilter_FilteredMessagesReceived()
        {
            var publisher = GetPublisher(_filteredSubscriber);

            publisher.Publish(new TestEventOne { Message = "yes" });
            publisher.Publish(new TestEventOne { Message = "no" });
            await publisher.PublishAsync(new TestEventOne { Message = "yes" });
            await publisher.PublishAsync(new TestEventOne { Message = "no" });

            _filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task Publish_EnvelopesWithFilter_FilteredEnvelopesAndMessagesReceived()
        {
            var publisher = GetPublisher(_filteredSubscriber);

            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "yes" }));
            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "no" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "yes" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "no" }));

            _filteredSubscriber.ReceivedEnvelopesCount.Should().Be(2);
            _filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }
    }
}