// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Configuration
{
    [Collection("StaticInMemory")]
    public class DependencyInjectionExtensionsTests
    {
        private readonly IServiceCollection _services;
        private readonly TestSubscriber _testSubscriber;
        private IServiceProvider _serviceProvider;

        private IServiceProvider GetServiceProvider() => _serviceProvider ?? (_serviceProvider = _services.BuildServiceProvider());

        private TestBroker GetBroker() => (TestBroker) GetServiceProvider().GetService<IBroker>();
        private IPublisher GetPublisher() => GetServiceProvider().GetService<IPublisher>();

        private BusConfigurator GetBusConfigurator() => GetServiceProvider().GetService<BusConfigurator>();
        private InMemoryOutboundQueue GetOutboundQueue() => (InMemoryOutboundQueue)GetServiceProvider().GetService<IOutboundQueueProducer>();

        private IInboundConnector GetInboundConnector() => GetServiceProvider().GetService<IInboundConnector>();
        private InMemoryInboundLog GetInboundLog() => (InMemoryInboundLog)GetServiceProvider().GetService<IInboundLog>();

        public DependencyInjectionExtensionsTests()
        {
            _services = new ServiceCollection();

            _services.AddBus();

            _testSubscriber = new TestSubscriber();
            _services.AddSingleton<ISubscriber>(_testSubscriber);

            _services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _services.AddSingleton<IPublisher, Publisher>();

            _serviceProvider = null; // Creation deferred to after AddBroker() has been called

            InMemoryInboundLog.Clear();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void AddBroker_BrokerRegisteredForDI()
        {
            _services.AddBroker<TestBroker>(options => { });

            GetServiceProvider().GetService<IBroker>().Should().NotBeNull();
        }

        [Fact]
        public void AddOutboundConnector_PublishMessages_MessagesProduced()
        {
            _services.AddBroker<TestBroker>(options => options.AddOutboundConnector());
            GetBusConfigurator().Connect(endpoints =>
                endpoints.AddOutbound<IIntegrationMessage>(TestEndpoint.Default));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());

            GetBroker().ProducedMessages.Count.Should().Be(5);
        }

        [Fact]
        public void AddDeferredOutboundConnector_PublishMessages_MessagesQueued()
        {
            _services.AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector<InMemoryOutboundQueue>());
            GetBusConfigurator().Connect(endpoints => 
                endpoints.AddOutbound<IIntegrationMessage>(TestEndpoint.Default));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCompletedEvent());

            GetOutboundQueue().Length.Should().Be(5);
        }

        [Fact]
        public void AddDeferredOutboundConnector_Rollback_MessagesNotQueued()
        {
            _services.AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector<InMemoryOutboundQueue>());
            GetBusConfigurator().Connect(endpoints =>
                endpoints.AddOutbound<IIntegrationMessage>(TestEndpoint.Default));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCompletedEvent());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionAbortedEvent());

            GetOutboundQueue().Length.Should().Be(2);
        }

        [Fact]
        public void AddOutboundRoutingTest()
        {
            _services.AddBroker<TestBroker>(options => options.AddOutboundConnector());

            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddOutbound<TestEventOne>(new TestEndpoint("test1"))
                    .AddOutbound<IIntegrationEvent>(new TestEndpoint("test2")));

            // -> to both endpoints
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventOne());
            // -> to test2
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            // -> to nowhere
            GetPublisher().Publish(new TestInternalEventOne());

            GetBroker().ProducedMessages.Count.Should().Be(7);
            GetBroker().ProducedMessages.Count(x => x.Endpoint.Name == "test1").Should().Be(2);
            GetBroker().ProducedMessages.Count(x => x.Endpoint.Name == "test2").Should().Be(5);
        }

        [Fact]
        public void AddInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddBroker<TestBroker>(options => options.AddInboundConnector());
            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(TestEndpoint.Default));

            var consumer = GetBroker().Consumers.First();
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }

        [Fact]
        public void AddLoggedInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddBroker<TestBroker>(options => options.AddLoggedInboundConnector<InMemoryInboundLog>());
            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(TestEndpoint.Default));

            var consumer = GetBroker().Consumers.First();
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            _testSubscriber.ReceivedMessages.Count.Should().Be(5);
        }
    }
}
