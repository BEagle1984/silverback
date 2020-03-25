// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

// ReSharper disable EmptyGeneralCatchClause

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    public class LoggedInboundConnectorTests
    {
        private readonly TestSubscriber _testSubscriber;
        private readonly IInboundConnector _connector;
        private readonly TestBroker _broker;
        private readonly IServiceProvider _serviceProvider;

        public LoggedInboundConnectorTests()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>())
                .AddSingletonSubscriber(_testSubscriber);

            services.AddScoped<IInboundLog, InMemoryInboundLog>();

            _serviceProvider = services.BuildServiceProvider();
            _broker = (TestBroker) _serviceProvider.GetService<IBroker>();
            _connector = new LoggedInboundConnector(
                _serviceProvider.GetService<IBrokerCollection>(),
                _serviceProvider,
                new NullLogger<LoggedInboundConnector>(),
                new MessageLogger());

            InMemoryInboundLog.Clear();
        }

        [Fact]
        public async Task Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            await consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);

            _testSubscriber.ReceivedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);

            _testSubscriber.ReceivedMessages.Count.Should().Be(6);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);
            await consumer.TestPush(e2);
            await consumer.TestPush(e1);

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e3 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e4 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventTwo m2 && m2.Id == e4.Id;

            var consumer = _broker.Consumers.First();

            try
            {
                await consumer.TestPush(e1);
            }
            catch
            {
            }

            try
            {
                await consumer.TestPush(e2);
            }
            catch
            {
            }

            try
            {
                await consumer.TestPush(e3);
            }
            catch
            {
            }

            try
            {
                await consumer.TestPush(e4);
            }
            catch
            {
            }

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact, Trait("CI", "false")]
        public async Task Bind_PushMessagesInBatchToMultipleConsumers_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e3 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e4 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                },
                Consumers = 2
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventTwo m2 && m2.Id == e2.Id;

            var consumer1 = _broker.Consumers[0];
            var consumer2 = _broker.Consumers[1];

            var tasks = new[]
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await consumer1.TestPush(e1);
                        await consumer1.TestPush(e2);
                    }
                    catch (Exception)
                    {
                    }
                }),
                Task.Run(async () =>
                {
                    await consumer2.TestPush(e3);
                    await consumer2.TestPush(e4);
                })
            };

            await Task.WhenAll(tasks);

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }
    }
}