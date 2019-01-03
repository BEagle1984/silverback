// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    [TestFixture]
    public class LoggedInboundConnectorTests
    {
        private IInboundLog _inboundLog;
        private TestSubscriber _testSubscriber;
        private IInboundConnector _connector;
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            services.AddSingleton<ISubscriber>(_testSubscriber);

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton<IPublisher, Publisher>();

            _broker = new TestBroker();
            _inboundLog = new InMemoryInboundLog();

            services.AddSingleton<IInboundLog>(_inboundLog);

            _connector = new LoggedInboundConnector(_broker, services.BuildServiceProvider(), new NullLogger<LoggedInboundConnector>());

            InMemoryInboundLog.Clear();
        }

        [Test]
        public void Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(2));
        }

        [Test]
        public void Bind_PushMessages_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(2));
        }

        [Test]
        public void Bind_PushMessages_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);

            Assert.That(_inboundLog.Length, Is.EqualTo(2));
        }

        [Test]
        public void Bind_PushMessagesInBatch_EachIsConsumedOnce()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(6));
        }

        [Test]
        public void Bind_PushMessagesInBatch_WrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);
            consumer.TestPush(e2);
            consumer.TestPush(e1);

            Assert.That(_inboundLog.Length, Is.EqualTo(2));
        }

        [Test]
        public void Bind_PushMessagesInBatch_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            });
            _broker.Connect();

            var consumer = _broker.Consumers.First();
            consumer.TestPush(e1);
            consumer.TestPush(e1);
            consumer.TestPush(e1);
            consumer.TestPush(e1);
            consumer.TestPush(e1);

            Assert.That(_inboundLog.Length, Is.EqualTo(1));
        }

        [Test]
        public void Bind_PushMessagesInBatchToMultipleConsumers_OnlyCommittedBatchWrittenToLog()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e3 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };
            var e4 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _connector.Bind(TestEndpoint.Default, settings: new InboundConnectorSettings
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                },
                Consumers = 2
            });
            _broker.Connect();

            _testSubscriber.FailCondition = m => m == e2;

            var consumer1 = _broker.Consumers[0];
            var consumer2 = _broker.Consumers[1];

            var tasks = new[]
            {
                Task.Run(() =>
                {
                    try
                    {
                        consumer1.TestPush(e1);
                        consumer1.TestPush(e2);
                    }
                    catch (Exception)
                    { }
                }),
                Task.Run(() =>
                {
                    consumer2.TestPush(e3);
                    consumer2.TestPush(e4);
                })
            };

            Task.WaitAll(tasks);

            Assert.That(_inboundLog.Length, Is.EqualTo(2));
        }
    }
}