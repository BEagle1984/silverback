// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            services.AddNullLogger();
            services
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>())
                .AddSingletonSubscriber(_testSubscriber);

            services.AddScoped<IInboundLog, InMemoryInboundLog>();

            _serviceProvider = services.BuildServiceProvider();
            _broker = (TestBroker) _serviceProvider.GetService<IBroker>();
            _connector = new LoggedInboundConnector(
                _serviceProvider.GetRequiredService<IBrokerCollection>(),
                _serviceProvider,
                _serviceProvider.GetRequiredService<ILogger<LoggedInboundConnector>>());
        }

        [Fact]
        public async Task Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = (TestConsumer) _broker.Consumers.First();
            await consumer.TestHandleMessage(
                new TestEventOne(), new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            await consumer.TestHandleMessage(
                new TestEventOne(), new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
                .Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "Test" };
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = (TestConsumer) _broker.Consumers.First();
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id2) });

            _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
                .Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessages_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "Test" };
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            _connector.Bind(TestConsumerEndpoint.GetDefault());
            _broker.Connect();

            var consumer = (TestConsumer) _broker.Consumers.First();
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id2) });

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "Test" };
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = (TestConsumer) _broker.Consumers.First();
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id2) });

            _testSubscriber.ReceivedMessages.Count(message => message is IIntegrationEvent)
                .Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "Test" };
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = (TestConsumer) _broker.Consumers.First();
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id1) });
            await consumer.TestHandleMessage(
                e2, new[] { new MessageHeader("x-message-id", id2) });
            await consumer.TestHandleMessage(
                e1, new[] { new MessageHeader("x-message-id", id2) });

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact]
        public async Task Bind_PushMessagesInBatch_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "Test" };
            var e3 = new TestEventTwo { Content = "Test" };
            var e4 = new TestEventTwo { Content = "FAIL" };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventTwo m2 && m2.Content == "FAIL";

            var consumer = (TestConsumer) _broker.Consumers.First();

            try
            {
                await consumer.TestHandleMessage(
                    e1, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            }
            catch
            {
            }

            try
            {
                await consumer.TestHandleMessage(
                    e2, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            }
            catch
            {
            }

            try
            {
                await consumer.TestHandleMessage(
                    e3, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            }
            catch
            {
            }

            try
            {
                await consumer.TestHandleMessage(
                    e4, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
            }
            catch
            {
            }

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }

        [Fact, Trait("CI", "false")]
        public async Task Bind_PushMessagesInBatchToMultipleConsumers_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test" };
            var e2 = new TestEventTwo { Content = "FAIL" };
            var e3 = new TestEventTwo { Content = "Test" };
            var e4 = new TestEventTwo { Content = "Test" };

            _connector.Bind(TestConsumerEndpoint.GetDefault(), settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                },
                Consumers = 2
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m is TestEventTwo m2 && m2.Content == "FAIL";

            var consumer1 = (TestConsumer) _broker.Consumers[0];
            var consumer2 = (TestConsumer) _broker.Consumers[1];

            var tasks = new[]
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await consumer1.TestHandleMessage(
                            e1, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
                        await consumer1.TestHandleMessage(
                            e2, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
                    }
                    catch (Exception)
                    {
                    }
                }),
                Task.Run(async () =>
                {
                    await consumer2.TestHandleMessage(
                        e3, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
                    await consumer2.TestHandleMessage(
                        e4, new[] { new MessageHeader("x-message-id", Guid.NewGuid()) });
                })
            };

            await Task.WhenAll(tasks);

            (await _serviceProvider.GetRequiredService<IInboundLog>().GetLength()).Should().Be(2);
        }
    }
}