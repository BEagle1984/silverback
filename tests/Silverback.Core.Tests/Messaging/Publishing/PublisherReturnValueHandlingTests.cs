// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherReturnValueHandlingTests
    {
        [Fact]
        public async Task Publish_SubscriberClassReturnsSingleMessage_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber<RepublishMessagesTestService>()
                    .AddDelegateSubscriber((TestCommandOne message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            republishedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsMultipleMessages_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber<RepublishMessagesTestService>()
                    .AddDelegateSubscriber((TestCommandOne message) => republishedMessages.Add(message))
                    .AddDelegateSubscriber((TestCommandTwo message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventTwo());

            republishedMessages.Should().HaveCount(4);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsSingleMessage_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => new TestCommandOne())
                    .AddDelegateSubscriber((TestCommandOne message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            republishedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task Publish_MultipleDelegateSubscribersReturnSingleMessage_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne _) => new TestCommandOne())
                    .AddDelegateSubscriber((TestEventOne _) => new TestCommandTwo())
                    .AddDelegateSubscriber((ICommand message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());

            republishedMessages.Should().HaveCount(4);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsMessagesArray_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (TestEventTwo _) => new ICommand[]
                        {
                            new TestCommandOne(),
                            new TestCommandTwo(),
                            new TestCommandOne()
                        })
                    .AddDelegateSubscriber((ICommand message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventTwo());

            republishedMessages.Should().HaveCount(6);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsMessagesList_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (TestEventTwo _) => new List<ICommand>
                        {
                            new TestCommandOne(),
                            new TestCommandTwo(),
                            new TestCommandOne()
                        })
                    .AddDelegateSubscriber((ICommand message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventTwo());

            republishedMessages.Should().HaveCount(6);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsMessagesEnumerable_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (TestEventTwo _) => new ICommand[]
                        {
                            new TestCommandOne(),
                            new TestCommandTwo(),
                            new TestCommandOne()
                        }.AsEnumerable())
                    .AddDelegateSubscriber((ICommand message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventTwo());

            republishedMessages.Should().HaveCount(6);
        }

        [Fact]
        public async Task Publish_MultipleDelegateSubscribersReturnMessagesEnumerable_MessagesRepublished()
        {
            var republishedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (TestEventTwo _) => new ICommand[]
                        {
                            new TestCommandOne(),
                            new TestCommandTwo(),
                            new TestCommandOne()
                        })
                    .AddDelegateSubscriber(
                        (TestEventTwo _) => new List<ICommand>
                        {
                            new TestCommandOne(),
                            new TestCommandTwo(),
                            new TestCommandOne()
                        })
                    .AddDelegateSubscriber((ICommand message) => republishedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventTwo());

            republishedMessages.Should().HaveCount(12);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsValues_ResultsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestCommandReplier>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<string>(new TestCommandWithReturnOne());
            var asyncResults = await publisher.PublishAsync<string>(new TestCommandWithReturnOne());

            syncResults.Should().BeEquivalentTo("response", "response2");
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsValuesOfWrongType_ResultsDiscarded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestCommandReplier>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<int>(new TestCommandWithReturnOne());
            var asyncResults = await publisher.PublishAsync<int>(new TestCommandWithReturnOne());

            syncResults.Should().BeEmpty();
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_SubscriberClassesReturnValuesOfMixedTypes_ResultsOfWrongTypeDiscarded()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber((TestCommandOne _) => "response")
                .AddDelegateSubscriber((TestCommandOne _) => 123));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<string>(new TestCommandOne());
            var asyncResults = await publisher.PublishAsync<string>(new TestCommandOne());

            syncResults.Should().BeEquivalentTo("response");
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsNull_EmptyResultReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestCommandReplierReturningNull>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<string>(new TestCommandWithReturnOne());
            var asyncResults = await publisher.PublishAsync<string>(new TestCommandWithReturnOne());

            syncResults.Should().BeEmpty();
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsEnumerable_CollectionOfEnumerableReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestCommandReplier>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnTwo());
            var asyncResults =
                await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnTwo());

            syncResults.Should().BeEquivalentTo(new object[] { new[] { "one", "two" } });
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_SubscriberClassReturnsEmptyEnumerable_CollectionOfEnumerableReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestCommandReplier>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnThree());
            var asyncResults =
                await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnThree());

            syncResults.Should().BeEquivalentTo(new object[] { Enumerable.Empty<string>() });
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsValue_ResultsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestCommandWithReturnOne _) => "response"));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<string>(new TestCommandWithReturnOne());
            var asyncResults = await publisher.PublishAsync<string>(new TestCommandWithReturnOne());

            syncResults.Should().BeEquivalentTo("response");
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_DelegateSubscriberReturnsValuesEnumerable_ResultsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (TestCommandWithReturnOne _) => new[] { "response1", "response2" }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnOne());
            var asyncResults =
                await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnOne());

            syncResults.Should().BeEquivalentTo(new object[] { new[] { "response1", "response2" } });
            asyncResults.Should().BeEquivalentTo(syncResults);
        }

        [Fact]
        public async Task Publish_MultipleDelegateSubscribersReturnValuesEnumerable_ResultsReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestCommandWithReturnOne _) => new[] { "response1", "response2" })
                    .AddDelegateSubscriber(
                        (TestCommandWithReturnOne _) => new[] { "response3", "response4" }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            var syncResults = publisher.Publish<IEnumerable<string>>(new TestCommandWithReturnOne());
            var asyncResults =
                await publisher.PublishAsync<IEnumerable<string>>(new TestCommandWithReturnOne());

            syncResults.Should().BeEquivalentTo(
                new object[]
                {
                    new[] { "response1", "response2" },
                    new[] { "response3", "response4" }
                });
            asyncResults.Should().BeEquivalentTo(syncResults);
        }
    }
}
