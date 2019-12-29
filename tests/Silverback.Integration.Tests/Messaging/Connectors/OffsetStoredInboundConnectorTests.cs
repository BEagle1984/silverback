// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    [SuppressMessage("ReSharper", "EmptyGeneralCatchClause")]
    public class OffsetStoredInboundConnectorTests
    {
        private readonly TestSubscriber _testSubscriber;
        private readonly IInboundConnector _connector;
        private readonly TestBroker _broker;
        private readonly IServiceProvider _scopedServiceProvider;

        public OffsetStoredInboundConnectorTests()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services
                .AddSilverback()
                .WithConnectionTo<TestBroker>()
                .AddSingletonSubscriber(_testSubscriber);

            services.AddScoped<IOffsetStore, InMemoryOffsetStore>();

            IServiceProvider serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            _broker = (TestBroker)serviceProvider.GetService<IBroker>();
            _connector = new OffsetStoredInboundConnector(_broker, serviceProvider, new NullLogger<OffsetStoredInboundConnector>(),
                new MessageLogger());

            _scopedServiceProvider = serviceProvider.CreateScope().ServiceProvider;

            InMemoryOffsetStore.Clear();
        }

        [Fact]
        public async Task Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() }, offset: new TestOffset("a", "1"));
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() }, offset: new TestOffset("a", "2"));

            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("a", "2");

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e1, offset: o1);
            await consumer.TestPush(e2, offset: o2);
            await consumer.TestPush(e1, offset: o1);
            await consumer.TestPush(e2, offset: o2);
            await consumer.TestPush(e1, offset: o1);

            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesFromDifferentTopics_EachIsConsumedOnce()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("b", "1");

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o1);

            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_OffsetStored()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("b", "1");
            var o3 = new TestOffset("a", "2");

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o3);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o1);

            _scopedServiceProvider.GetRequiredService<IOffsetStore>().As<InMemoryOffsetStore>().Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_EachIsConsumedOnce()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("b", "1");
            var o3 = new TestOffset("a", "2");

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o3);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o3);

            _testSubscriber.ReceivedMessages.OfType<TestEventOne>().Should().HaveCount(3);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_OffsetStored()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("b", "1");
            var o3 = new TestOffset("a", "2");

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e, offset: o1);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o3);
            await consumer.TestPush(e, offset: o2);
            await consumer.TestPush(e, offset: o1);

            _scopedServiceProvider.GetRequiredService<IOffsetStore>().As<InMemoryOffsetStore>().Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_OnlyOffsetOfCommittedBatchStored()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var fail = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("a", "2");
            var o3 = new TestOffset("a", "3");
            var o4 = new TestOffset("a", "4");

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventOne m2 && m2.Id == fail.Id;

            var consumer = _broker.Consumers.First();

            try { await consumer.TestPush(e, offset: o1); } catch { }
            try { await consumer.TestPush(e, offset: o2); } catch { }
            try { await consumer.TestPush(e, offset: o3); } catch { }
            try { await consumer.TestPush(fail, offset: o4); } catch { }

            (await _scopedServiceProvider.GetRequiredService<IOffsetStore>().GetLatestValue("a")).Value.Should().Be("2");
        }

        [Fact]
        public async Task Bind_PushMessagesInBatchToMultipleConsumers_OnlyOffsetOfCommittedBatchStored()
        {
            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var fail = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var o1 = new TestOffset("a", "1");
            var o2 = new TestOffset("a", "2");
            var o3 = new TestOffset("a", "3");
            var o4 = new TestOffset("a", "4");

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                },
                Consumers = 2
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventOne m2 && m2.Id == fail.Id;

            var consumer1 = _broker.Consumers[0];
            var consumer2 = _broker.Consumers[1];

            var tasks = new[]
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await consumer1.TestPush(e, offset: o1);
                        await consumer1.TestPush(e, offset: o2);
                    }
                    catch (Exception)
                    { }
                }),
                Task.Run(async () =>
                {
                    try
                    {
                        await consumer2.TestPush(e, offset: o3);
                        await consumer2.TestPush(fail,offset: o4);
                    }
                    catch (Exception)
                    {
                    }
                })
            };

            await Task.WhenAll(tasks);

            (await _scopedServiceProvider.GetRequiredService<IOffsetStore>().GetLatestValue("a")).Value.Should().Be("2");
        }
    }
}