// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherTests
    {
        private readonly TestSubscriber _syncSubscriber;

        private readonly TestAsyncSubscriber _asyncSubscriber;

        private readonly TestEnumerableSubscriber _syncEnumerableSubscriber;

        private readonly TestAsyncEnumerableSubscriber _asyncEnumerableSubscriber;

        private readonly TestReadOnlyCollectionSubscriber _syncReadOnlyCollectionSubscriber;

        private readonly TestAsyncReadOnlyCollectionSubscriber _asyncReadOnlyCollectionSubscriber;

        public PublisherTests()
        {
            _syncSubscriber = new TestSubscriber();
            _asyncSubscriber = new TestAsyncSubscriber();
            _syncEnumerableSubscriber = new TestEnumerableSubscriber();
            _asyncEnumerableSubscriber = new TestAsyncEnumerableSubscriber();
            _syncReadOnlyCollectionSubscriber = new TestReadOnlyCollectionSubscriber();
            _asyncReadOnlyCollectionSubscriber = new TestAsyncReadOnlyCollectionSubscriber();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
        public static IEnumerable<object[]> Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData =>
            new List<object[]>
            {
                new object[] { new TestEventOne(), 1, 0 },
                new object[] { new TestEventTwo(), 1, 1 }
            };

        [Fact]
        public void Publish_SomeMessages_Received()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = PublisherTestsHelper.GetPublisher(
                silverbackBuilder => silverbackBuilder
                    .AddDelegateSubscriber((object _) => count++));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = PublisherTestsHelper.GetPublisher(
                silverbackBuilder => silverbackBuilder
                    .AddDelegateSubscriber((object _) => count++));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = PublisherTestsHelper.GetPublisher(
                silverbackBuilder => silverbackBuilder
                    .AddDelegateSubscriber<object>((msg, sp) => sp != null ? count++ : count = 0));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = PublisherTestsHelper.GetPublisher(
                silverbackBuilder => silverbackBuilder
                    .AddDelegateSubscriber<object>(
                        (message, serviceProvider) => serviceProvider != null ? count++ : count = 0));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber, _asyncSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber, _asyncSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task Publish_SomeMessagesWithoutAutoSubscriptionOfPublicMethods_ReceivedByAllSubscribedMethods()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = PublisherTestsHelper.GetPublisher(
                silverbackBuilder => silverbackBuilder
                    .AddSingletonSubscriber(service1, autoSubscribeAllPublicMethods: false)
                    .AddSingletonSubscriber(service2, autoSubscribeAllPublicMethods: false));

            await publisher.PublishAsync(new TestCommandOne()); // service1 +2
            await publisher.PublishAsync(new TestCommandTwo()); // service2 +2
            publisher.Publish(new TestCommandOne()); // service1 +2
            await publisher.PublishAsync(new TransactionCompletedEvent()); // service1/2 +1
            publisher.Publish(new TransactionAbortedEvent()); // service1/2 +1

            service1.ReceivedMessagesCount.Should().Be(6);
            service2.ReceivedMessagesCount.Should().Be(4);
        }

        [Theory]
        [MemberData(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public void Publish_SubscribedMessage_ReceivedRepublishedMessages(
            IEvent message,
            int expectedEventOne,
            int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = PublisherTestsHelper.GetPublisher(new RepublishMessagesTestService(), service1, service2);

            publisher.Publish(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Theory]
        [MemberData(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(
            IEvent message,
            int expectedEventOne,
            int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = PublisherTestsHelper.GetPublisher(new RepublishMessagesTestService(), service1, service2);

            await publisher.PublishAsync(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Fact]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestExceptionSubscriber());

            Action act1 = () => publisher.Publish(new TestEventOne());
            Action act2 = () => publisher.Publish(new TestEventTwo());

            act1.Should().Throw<AggregateException>();
            act2.Should().Throw<AggregateException>();
        }

        [Fact]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestExceptionSubscriber());

            Func<Task> act1 = async () => await publisher.PublishAsync(new TestEventOne());
            Func<Task> act2 = async () => await publisher.PublishAsync(new TestEventTwo());

            act1.Should().Throw<TargetInvocationException>();
            act2.Should().Throw<TargetInvocationException>();
        }

        [Fact]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = publisher.Publish<string>(new TestCommandWitReturnOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public void Publish_HandlersReturnValueOfWrongType_EmptyResultReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = publisher.Publish<int>(new TestCommandWitReturnOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public void Publish_SomeHandlersReturnValueOfWrongType_ValuesOfCorrectTypeAreReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                new TestCommandReplier(),
                new TestCommandReplierWithWrongResponseType());

            var results = publisher.Publish<string>(new TestCommandWitReturnOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public void Publish_HandlersReturnNull_EmptyResultReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplierReturningNull());

            var results = publisher.Publish<string>(new TestCommandWitReturnOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = await publisher.PublishAsync<string>(new TestCommandWitReturnOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValueOfWrongType_EmptyResultReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = await publisher.PublishAsync<string>(new TestCommandWitReturnOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_SomeHandlersReturnValueOfWrongType_ValuesOfCorrectTypeAreReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                new TestCommandReplier(),
                new TestCommandReplierWithWrongResponseType());

            var results = await publisher.PublishAsync<string>(new TestCommandWitReturnOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnNull_EmptyResultReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplierReturningNull());

            var results = await publisher.PublishAsync<string>(new TestCommandWitReturnOne());

            results.Should().BeEmpty();
        }

        [Fact]
        public void Publish_HandlersReturnValue_EnumerableReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnTwo());

            results.SelectMany(x => x).Should().Equal("one", "two");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_EnumerableReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnTwo());

            results.SelectMany(x => x).Should().Equal("one", "two");
        }

        [Fact]
        public void Publish_HandlersReturnValue_EmptyEnumerableReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnThree());

            results.First().Should().NotBeNull();
            results.First().Should().BeEmpty();
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_EmptyEnumerableReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnThree());

            results.First().Should().NotBeNull();
            results.First().Should().BeEmpty();
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedAsEnumerable()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(
                _syncReadOnlyCollectionSubscriber,
                _asyncReadOnlyCollectionSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(
                _syncReadOnlyCollectionSubscriber,
                _asyncReadOnlyCollectionSubscriber);

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
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_EachMessageReceived()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsEnumerable()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedAsEnumerable()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedAsReadOnlyCollection()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                _syncReadOnlyCollectionSubscriber,
                _asyncReadOnlyCollectionSubscriber);

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedAsReadOnlyCollection()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                _syncReadOnlyCollectionSubscriber,
                _asyncReadOnlyCollectionSubscriber);

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            _syncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncReadOnlyCollectionSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncReadOnlyCollectionSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_ResultsReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = await publisher.PublishAsync<IEnumerable<string>>(
                new[]
                {
                    new TestCommandWithReturnTwo(), new TestCommandWithReturnTwo()
                });

            results.SelectMany(x => x).Should().Equal("one", "two", "one", "two");
        }

        [Fact]
        public void Publish_MessagesBatch_ResultsReturned()
        {
            var publisher = PublisherTestsHelper.GetPublisher(new TestCommandReplier());

            var results = publisher.Publish<IEnumerable<string>>(
                new[]
                {
                    new TestCommandWithReturnTwo(), new TestCommandWithReturnTwo()
                });

            results.SelectMany(x => x).Should().Equal("one", "two", "one", "two");
        }

        [Fact]
        public void Publish_MessagesBatch_EachMessageReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((ITestMessage msg) => receivedMessagesCount++)
                        .AddDelegateSubscriber(
                            async (ITestMessage msg) =>
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

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((ITestMessage msg) => receivedMessagesCount++)
                        .AddDelegateSubscriber(
                            async (ITestMessage msg) =>
                            {
                                await Task.Delay(1);
                                asyncReceivedMessagesCount++;
                            }));

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                        .AddDelegateSubscriber(
                            async (IEnumerable<ITestMessage> msg) =>
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

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                        .AddDelegateSubscriber(
                            async (IEnumerable<ITestMessage> msg) =>
                            {
                                await Task.Delay(1);
                                asyncReceivedMessagesCount += msg.Count();
                            }));

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()
                });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_NewMessagesReturnedByDelegateSubscription_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((TestCommandTwo msg) => new TestCommandOne())
                        .AddDelegateSubscriber(
                            async (TestCommandTwo msg) =>
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
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public async Task PublishAsync_ExclusiveSubscribers_SequentiallyInvoked()
        {
            var subscriber = new ExclusiveSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task PublishAsync_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public void Publish_ExclusiveDelegateSubscription_SequentiallyInvoked()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber((ICommand _) => parallel.DoWork())
                        .AddDelegateSubscriber(
                            async (ICommand _) => await parallel.DoWorkAsync(),
                            new SubscriptionOptions { Exclusive = true }));

            publisher.Publish(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(1, 2);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_NonExclusiveDelegateSubscription_InvokedInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWork(),
                            new SubscriptionOptions { Exclusive = false })
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWorkAsync(),
                            new SubscriptionOptions { Exclusive = false }));

            publisher.Publish(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(1, 1);
        }

        [Fact]
        public void Publish_NonParallelSubscriber_SequentiallyProcessing()
        {
            var subscriber = new NonParallelSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 2, 3, 4);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_ParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new ParallelSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            publisher.Publish(
                new ICommand[]
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

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWork(),
                            new SubscriptionOptions { Parallel = false })
                        .AddDelegateSubscriber(
                            async (ICommand _) => await parallel.DoWorkAsync(),
                            new SubscriptionOptions { Parallel = false }));

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                });

            parallel.Steps.Should().BeEquivalentTo(1, 2, 3, 4);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_ParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWork(),
                            new SubscriptionOptions { Parallel = true })
                        .AddDelegateSubscriber(
                            async (ICommand _) => await parallel.DoWorkAsync(),
                            new SubscriptionOptions { Parallel = true }));

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                });

            parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 3);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 3);
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task PublishAsync_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = PublisherTestsHelper.GetPublisher(subscriber);

            await publisher.PublishAsync(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                    new TestCommandOne(),
                });

            subscriber.Parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 4, 4, 6);
        }

        [Fact]
        [Trait("CI", "false")]
        public void Publish_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWork(),
                            new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                        .AddDelegateSubscriber(
                            async (ICommand _) => await parallel.DoWorkAsync(),
                            new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            publisher.Publish(
                new ICommand[]
                {
                    new TestCommandOne(),
                    new TestCommandTwo(),
                    new TestCommandOne(),
                });

            parallel.Steps.Should().BeEquivalentTo(1, 1, 3, 4, 4, 6);
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task PublishAsync_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var parallel = new ParallelTestingUtil();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder
                        .AddDelegateSubscriber(
                            (ICommand _) => parallel.DoWork(),
                            new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                        .AddDelegateSubscriber(
                            async (ICommand _) => await parallel.DoWorkAsync(),
                            new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            await publisher.PublishAsync(
                new ICommand[]
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
            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { new TestBehavior(), new TestBehavior() },
                _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_WithBehaviors_MessagesReceived()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { new TestBehavior(), new TestBehavior() },
                _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_WithBehaviors_BehaviorsExecuted()
        {
            var behavior1 = new TestBehavior();
            var behavior2 = new TestBehavior();

            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { behavior1, behavior2 },
                _asyncSubscriber);

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

            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { behavior1, behavior2 },
                _asyncSubscriber);

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

            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { behavior1, behavior2, behavior3, behavior4, behavior5 },
                _asyncSubscriber);

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

            var publisher = PublisherTestsHelper.GetPublisher(
                null,
                new IBehavior[] { behavior1, behavior2, behavior3, behavior4, behavior5 },
                _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());

            callsSequence.Should().BeEquivalentTo(
                new[] { "-100", "-50", "unsorted", "50", "100" },
                opt => opt.WithStrictOrdering());
        }

        [Fact]
        public void Publish_MessageChangingBehavior_BehaviorExecuted()
        {
            var behavior = new ChangeMessageBehavior<TestCommandOne>(
                _ => new[]
                {
                    new TestCommandTwo(), new TestCommandTwo(), new TestCommandTwo()
                });

            var publisher = PublisherTestsHelper.GetPublisher(null, new IBehavior[] { behavior }, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(4);
        }

        [Fact]
        public async Task PublishAsync_MessageChangingBehavior_BehaviorExecuted()
        {
            var behavior = new ChangeMessageBehavior<TestCommandOne>(
                _ => new[]
                {
                    new TestCommandTwo(), new TestCommandTwo(), new TestCommandTwo()
                });

            var publisher = PublisherTestsHelper.GetPublisher(null, new IBehavior[] { behavior }, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _asyncSubscriber.ReceivedMessagesCount.Should().Be(4);
        }

        [Fact]
        public void Publish_AutoSubscribing_AllMethodsInvoked()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            publisher.Publish(new TestEventOne());

            _syncSubscriber.ReceivedCallsCount.Should().Be(3);
        }

        [Fact]
        public void Publish_AutoSubscribingDisabled_OnlyDecoratedMethodsInvoked()
        {
            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddSingletonSubscriber(_syncSubscriber, false));

            publisher.Publish(new TestEventOne());

            _syncSubscriber.ReceivedCallsCount.Should().Be(2);
        }

        [Fact]
        public void Publish_GenericSubscriber_MessageReceived()
        {
            var subscriber = new EventOneGenericSubscriber();
            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddSingletonSubscriber(subscriber, false));

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameter_MethodInvokedForMatchingMessages()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(
                    builder => builder.AddDelegateSubscriber((TestEventOne x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameter_MethodInvokedForMatchingMessages()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(
                    builder => builder.AddDelegateSubscriber((TestEventOne x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameterOfAncestorType_MethodInvoked()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddDelegateSubscriber((IEvent x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameterOfAncestorType_MethodInvoked()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddDelegateSubscriber((IEvent x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(3);
        }

        [Fact]
        public void Publish_SubscriberWithSingleMessageParameterOfNotMatchingType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddDelegateSubscriber((ICommand x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithSingleMessageParameterOfNotMatchingType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher =
                PublisherTestsHelper.GetPublisher(builder => builder.AddDelegateSubscriber((ICommand x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public void Publish_SubscriberWithEnumerableParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder.AddDelegateSubscriber((IEnumerable<TestEventOne> x) => receivedMessages += x.Count()));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithEnumerableParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder.AddDelegateSubscriber((IEnumerable<TestEventOne> x) => receivedMessages += x.Count()));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberWithEnumerableParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder.AddDelegateSubscriber((IEnumerable<IEvent> x) => receivedMessages += x.Count()));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_SubscriberWithEnumerableParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder =>
                    builder.AddDelegateSubscriber((IEnumerable<IEvent> x) => receivedMessages += x.Count()));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            receivedMessages.Should().Be(3);
        }

        [Fact]
        public void Publish_NoMessagesMatchingEnumerableType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder.AddDelegateSubscriber((IEnumerable<ICommand> x) => calls++));

            publisher.Publish(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        public async Task PublishAsync_NoMessagesMatchingEnumerableType_MethodNotInvoked()
        {
            var calls = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder.AddDelegateSubscriber((IEnumerable<ICommand> x) => calls++));

            await publisher.PublishAsync(new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() });

            calls.Should().Be(0);
        }

        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
        public async Task PublishAsync_MultipleRegisteredSubscribers_OnlyMatchingSubscribersResolvedAfterFirstPublish()
        {
            var resolved = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                builder => builder
                    .AddSilverback()
                    .AddScopedSubscriber(
                        _ =>
                        {
                            resolved++;
                            return new TestServiceOne();
                        })
                    .AddScopedSubscriber(
                        _ =>
                        {
                            resolved++;
                            return new TestServiceTwo();
                        }));

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(2);

            // Publish single message
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish enumerable of messages
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new[] { new TestCommandOne() });
            }

            resolved.Should().Be(1);

            // Publish stream
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new[] { new MessageStreamEnumerable<TestCommandOne>() })
                    .RunWithTimeout();
            }

            resolved.Should().Be(0);
        }

        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Test")]
        public async Task PublishAsync_MultipleRegisteredSubscribersWithPreloading_OnlyMatchingSubscribersAreResolved()
        {
            var resolved = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                builder => builder
                    .AddSilverback()
                    .AddScopedSubscriber(
                        _ =>
                        {
                            resolved++;
                            return new TestServiceOne();
                        })
                    .AddScopedSubscriber(
                        _ =>
                        {
                            resolved++;
                            return new TestServiceTwo();
                        }));

            await serviceProvider.GetServices<IHostedService>()
                .OfType<SubscribedMethodsLoaderService>()
                .Single()
                .StartAsync(CancellationToken.None);

            resolved.Should().Be(2);

            // Publish single message
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new TestCommandOne());
            }

            resolved.Should().Be(1);

            // Publish enumerable of messages
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new[] { new TestCommandOne() });
            }

            resolved.Should().Be(1);

            // Publish stream
            resolved = 0;

            using (var scope = serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new[] { new MessageStreamEnumerable<TestCommandOne>() })
                    .RunWithTimeout();
            }

            resolved.Should().Be(0);
        }

        [Fact]
        public void Publish_SubscribedBaseType_MethodsInvoked()
        {
            var testService1 = new TestServiceOne();
            var testService2 = new TestServiceTwo();

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddSubscribers<IService>()
                    .Services
                    .AddSingleton<IService>(testService1)
                    .AddSingleton(testService1)
                    .AddSingleton<IService>(testService2)
                    .AddSingleton(testService2));

            publisher.Publish(new object[] { new TestCommandOne(), new TestCommandTwo() });

            testService1.ReceivedMessagesCount.Should().BeGreaterThan(0);
            testService2.ReceivedMessagesCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Publish_SubscriberNotRegisteredAsSelf_InvalidOperationExceptionIsThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddSubscribers<IService>()
                    .Services
                    .AddScoped<IService>(_ => new TestServiceOne())
                    .AddScoped<IService>(_ => new TestServiceTwo()));

            Action act = () => publisher.Publish(new object[] { new TestCommandOne(), new TestCommandTwo() });

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Publish_NotSubscribedMessageWithThrowIfUnhandled_ExceptionThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            Action act = () => publisher.Publish(new UnhandledMessage(), true);

            act.Should().ThrowExactly<AggregateException>();
        }

        [Fact]
        public void PublishAsync_NotSubscribedMessageWithThrowIfUnhandled_ExceptionThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            Func<Task> act = () => publisher.PublishAsync(new UnhandledMessage(), true);

            act.Should().ThrowExactly<UnhandledMessageException>();
        }

        [Fact]
        public void Publish_NotSubscribedMessageWithoutThrowIfUnhandled_ExceptionThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            Action act = () => publisher.Publish(new UnhandledMessage());

            act.Should().NotThrow();
        }

        [Fact]
        public void PublishAsync_NotSubscribedMessageWithoutThrowIfUnhandled_ExceptionThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher(_syncSubscriber);

            Func<Task> act = () => publisher.PublishAsync(new UnhandledMessage());

            act.Should().NotThrow();
        }

        [Fact]
        public void PublishAsync_WithoutSubscribersDisablingException_NoExceptionThrown()
        {
            var publisher = PublisherTestsHelper.GetPublisher();

            Func<Task> act = () => publisher.PublishAsync(new TestCommandOne());

            act.Should().NotThrow();
        }
    }
}
